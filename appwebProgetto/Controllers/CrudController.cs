using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using School.Data;
using System;
using System.Threading.Tasks;

namespace School.Controllers
{
    public abstract class CrudController<TContext, TId, TEntity> : Controller
        where TContext : DbContext
        where TEntity : class
    {
        protected readonly TContext Context;
        protected readonly ILogger Logger;
        protected static readonly IActionResult EmptyJson = new JsonResult(new { });

        protected abstract DbSet<TEntity> Entities { get; }

        protected abstract Func<TEntity, TId, bool> FilterById { get; }

        protected CrudController(TContext context, ILogger logger)
        {
            Context = context;
            Logger = logger;
        }

        public virtual IActionResult Index() => View();

        [HttpGet]
        public virtual async Task<IActionResult> Read()
        {
            var data = Entities;

            return Json(new DataSourceResult
            {
                Total = await data.CountAsync(),
                Data = data
            });
        }

        [HttpPost]
        public virtual async Task<IActionResult> Create(TEntity entity)
        {
            Context.Add(entity);
            await Context.SaveChangesAsync();

            return EmptyJson;
        }

        [HttpPost]
        public virtual async Task<IActionResult> Update(TEntity entity)
        {
            Context.Update(entity);
            Context.Entry(entity).State = EntityState.Modified;
            await Context.SaveChangesAsync();

            return EmptyJson;
        }

        [HttpPost]
        public virtual async Task<IActionResult> Delete(TId id)
        {
            var entity = await Entities.SingleOrDefaultAsync(s => FilterById(s, id));
            if (entity == null)
            {
                return NotFound();
            }

            Context.Remove(entity);
            await Context.SaveChangesAsync();

            return EmptyJson;
        }
    }
}