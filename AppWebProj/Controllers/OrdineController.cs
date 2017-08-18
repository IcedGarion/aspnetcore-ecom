using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Upo.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Upo.Model;
using Microsoft.AspNetCore.Http;

namespace Upo.Controllers
{
    public class OrdineController : CrudController<UpoECommerceContext, int, Ordine>
    {
        public OrdineController(UpoECommerceContext context, ILogger<OrdineController> logger) : base(context, logger)
        {
        }

        protected override DbSet<Ordine> Entities => Context.Ordine;

        protected override Func<Ordine, int, bool> FilterById => (e, id) => e.CdOrdine == id;

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            UpoECommerceContext context = new UpoECommerceContext();

            //Se non si e' loggati redirige alla login, quando si tenta di acquistare
            if(HttpContext.Session.GetInt32("CdUtente") == null)
            {
                return Redirect("/Utente/Login");
            }

            //legge il carrello
            var SessionCart = HttpContext.Session.GetObjectFromJson<List<OrdineProdotto>>("Cart");
            if (SessionCart == null)
            {
                return Redirect("/Carrello/Index");
            }

            //legge cdUtente
            int utente = 0;
            if (HttpContext.Session.GetInt32("CdUtente") != null)
                utente = (int)HttpContext.Session.GetInt32("CdUtente");

            //cerca nel db tutti i prodotti da inserire, per calcolare il totale
            var AddProductsDb = from prodotti in context.Prodotto
                              join carrello in SessionCart on prodotti.CdProdotto equals carrello.CdProdotto
                              select new { Prezzo = prodotti.Prezzo, Sconto = prodotti.Sconto, Quantita = carrello.Quantita };

            double totale = AddProductsDb.Sum(x => (x.Prezzo - x.Sconto) * x.Quantita);

            //crea elenco ordineProdotto
            List<OrdineProdotto> ordProd = new List<OrdineProdotto>();

            foreach (var prod in SessionCart)
            {
                ordProd.Add(new OrdineProdotto
                {
                    CdProdotto = prod.CdProdotto,
                    Quantita = prod.Quantita
                });
            }

            //crea un nuovo ordine collegato all'utente e all'elenco di prodotti sopra
            Ordine ordine = new Ordine
            {
                CdUtente = utente,
                Stato = "Sent",
                DtInserimento = DateTime.Now,
                Totale = totale,
                OrdineProdotto = ordProd
            };

            //salva sul db
            await base.Create(ordine);

            //rimuove carrello in session
            HttpContext.Session.Remove("Cart");

            return Redirect("/Ordine/Index");
        }

        [HttpPost]
        public async Task<IActionResult> Update(string ordine, string stato)
        {
            //riceve parametri dal form
            Int32.TryParse(ordine, out int CdOrdine);
            Ordine ToUpdate;

            //cerca nel db quell'ordine
            var query = from ordini in Context.Ordine
                        where ordini.CdOrdine.Equals(CdOrdine)
                        select ordini;

            //prende il primo elemento (l'unico) della query
            ToUpdate = query.First();

            //modifica stato solo se diverso!
            if (!ToUpdate.Stato.Equals(stato))
            {
                ToUpdate.Stato = stato;

                //salva su db
                await base.Update(ToUpdate);
            }

            return Redirect("/Ordine/List");
        }


        //FILTRA
        public async Task<IActionResult> Index(string clear, string start, string end,
            string titolo, string qtaoperator, string qta, string totoperator, string tot, string stato)
        {
            //prende cdUtente da session
            var tmp = HttpContext.Session.GetInt32("CdUtente");
            var ruolo = HttpContext.Session.GetString("Ruolo");
            int CdUtente = (int)tmp;
            var Query = UserQuery(CdUtente);
            bool filtered = false;

            //FILTRA
            Query = Query.FilterOrder(ref filtered, clear, start, end, titolo, qtaoperator, qta, totoperator, tot, stato);

            TempData["OrdineFilter"] = filtered.ToString();

            return View(await Query.ToListAsync());
        }

        /***
         * GET senza parametri: lista normale; GET con parametro: filtra secondo i parametri
         ***/
        [HttpGet]
        public async Task<IActionResult> List(string clear, string start, string end,
            string titolo, string qtaoperator, string qta, string totoperator, string tot, string stato)
        {
            var Query = AdminQuery();
            bool filtered = false;

            //FILTRA
            Query = Query.FilterOrder(ref filtered, clear, start, end, titolo, qtaoperator, qta, totoperator, tot, stato);

            TempData["OrdineFilter"] = filtered.ToString();

            return View(await Query.ToListAsync());
        }

        

        private IQueryable<OrdiniJoinDataSource> AdminQuery()
        {
            var q = from ordini in Context.Ordine
                    join utenti in Context.Utente on ordini.CdUtente equals utenti.CdUtente
                    join ordineProdotto in Context.OrdineProdotto on ordini.CdOrdine equals ordineProdotto.CdOrdine
                    join prodotti in Context.Prodotto on ordineProdotto.CdProdotto equals prodotti.CdProdotto
                    select new OrdiniJoinDataSource
                    {
                        CdOrdine = ordini.CdOrdine,
                        Stato = ordini.Stato,
                        Username = utenti.Username,
                        Titolo = prodotti.Titolo,
                        DtInserimento = ordini.DtInserimento,
                        Quantita = ordineProdotto.Quantita,
                        Totale = ordini.Totale
                    };

            return q;
        }

        private IQueryable<OrdiniJoinDataSource> UserQuery(int CdUtente)
        {
            //invece di restituire solo gli ordini, fa una join per aggiungere altre informazioni
            var q = from ordini in Context.Ordine
                    join utenti in Context.Utente on ordini.CdUtente equals utenti.CdUtente
                    join ordineProdotto in Context.OrdineProdotto on ordini.CdOrdine equals ordineProdotto.CdOrdine
                    join prodotti in Context.Prodotto on ordineProdotto.CdProdotto equals prodotti.CdProdotto
                    where utenti.CdUtente.Equals(CdUtente)
                    select new OrdiniJoinDataSource
                    {
                        CdOrdine = ordini.CdOrdine,
                        Stato = ordini.Stato,
                        Username = utenti.Username,
                        Titolo = prodotti.Titolo,
                        DtInserimento = ordini.DtInserimento,
                        Quantita = ordineProdotto.Quantita,
                        Totale = ordini.Totale
                    };
            return q;
        }

    }
}