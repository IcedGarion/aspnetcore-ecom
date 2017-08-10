using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using School.Controllers;
using School.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using School.Model;
using Microsoft.AspNetCore.Http;

namespace School.Controllers
{
    public class OrdineController : CrudController<SchoolContext, int, Ordine>
    {
        public OrdineController(SchoolContext context, ILogger<OrdineController> logger) : base(context, logger)
        {
        }

        protected override DbSet<Ordine> Entities => Context.Ordine;

        protected override Func<Ordine, int, bool> FilterById => (e, id) => e.CdOrdine == id;

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Ordine());
        }

        [HttpPost]
        public async Task<IActionResult> Create(Ordine ord)
        {
            await base.Create(ord);

            return Redirect("/Ordine");
        }

        //passa alla view la lista di tutte le entites del controller (Context.Ordine)
        public IActionResult List()
        {
            SchoolContext context = new SchoolContext();

            //invece di restituire solo gli ordini, fa una join per aggiungere altre informazioni
            var query = from ordini in context.Ordine
                        join utenti in context.Utente on ordini.CdUtente equals utenti.CdUtente
                        join ordineProdotto in context.OrdineProdotto on ordini.CdOrdine equals ordineProdotto.CdOrdine
                        join prodotti in context.Prodotto on ordineProdotto.CdProdotto equals prodotti.CdProdotto
                        select new OrdiniJoinDataSource { CdOrdine = ordini.CdOrdine, Username = utenti.Username, Titolo = prodotti.Titolo, Totale = ordini.Totale };

            return View(query.ToList());
        }

        public IActionResult Add(string input)
        {
            //aggiunge al carrello (in session) il prodotto con CdProdotto = input
            HttpContext.Session.SetString("Cart", input);

            var session = HttpContext.Session.GetString("Cart");

            Console.WriteLine(session);

            return Redirect("/Home/Index");
        }

        public IActionResult Index() => Redirect("/Ordine/List");

    }
}