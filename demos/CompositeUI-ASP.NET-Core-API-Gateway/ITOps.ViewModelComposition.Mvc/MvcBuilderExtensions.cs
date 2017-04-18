﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace ITOps.ViewModelComposition.Mvc
{
    public static class MvcBuilderExtensions
    {
        public static IMvcBuilder AddViewModelCompositionMvcSupport(this IMvcBuilder builder)
        {
            builder.Services.Configure<MvcOptions>(options => 
            {
                options.Filters.Add(typeof(CompositionActionFilter));
            });

            return builder;
        }
    }
}
