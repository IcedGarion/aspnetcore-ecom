using Microsoft.AspNetCore.Mvc;
using School.Data;
using System.Linq;

namespace School.Controllers
{
    public class OffertaController : Controller
    {
        public IActionResult Index()
        {
            SchoolContext context = new SchoolContext();

            var query = from prodotto in context.Prodotto
                        where prodotto.Sconto > 0
                        select prodotto;

            return View(query.ToList());
        }
    }
}
