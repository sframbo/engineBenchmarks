﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using DotLiquid;
using HandlebarsDotNet;
using Scriban.Functions;
using Scriban.Runtime;
using Scriban;

namespace ConsoleApp3
{
    internal class benchmarks
    {
    }

    [MemoryDiagnoser]
    [HtmlExporter]
    [RPlotExporter]
    public class BenchParsers
    {
        public BenchParsers()
        {
            // Due to issue https://github.com/rexm/Handlebars.Net/issues/105 cannot do the same as others, so
            // working around this here
            HandlebarsDotNet.Handlebars.RegisterHelper("truncate", (output, options, context, arguments) => {
                output.Write(Scriban.Functions.StringFunctions.Truncate((string)context["description"], 15));
            });
        }

        // FluidParser instance is meant to be global
        private static readonly Fluid.FluidParser FluidParser = new Fluid.FluidParser();

        protected const string TextTemplateDotLiquid = @"
            <ul id='products'>
              {% for product in products %}
                <li>
                  <h2>{{ product.name }}</h2>
                       Only {{ product.price }}
                       {{ product.description | truncate: 15 }}
                </li>
              {% endfor %}
            </ul>
            ";

        protected const string TextTemplateScriban = @"
            <ul id='products'>
              {{ for product in products; with product }}
                <li>
                  <h2>{{ name }}</h2>
                       Only {{ price }}
                       {{ truncate description 15 }}
                </li>
              {{ end; end }}
            </ul>
            ";

        public const string TextTemplateMustache = @"
            <ul id='products'>
              {{#products}}
                <li>
                  <h2>{{ name }}</h2>
                       Only {{ price }}
                       {{#truncate}}{{description}}{{/truncate}}
                </li>
              {{/products}}
            </ul>
            ";

        public const string TextTemplateCottle = @"
            <ul id='products'>
              { for product in products:
                <li>
                  <h2>{ product.Name }</h2>
                       Only { product.Price }
                       { string.truncate(product.Description, 15) }
                </li>
              }
            </ul>
            ";

        [Benchmark(Description = "Scriban")]
        public Scriban.Template TestScriban()
        {
            return Scriban.Template.Parse(TextTemplateScriban);
        }

        [Benchmark(Description = "Scriban Liquid")]
        public Scriban.Template TestScribanLiquid()
        {
            return Scriban.Template.ParseLiquid(TextTemplateDotLiquid);
        }

        [Benchmark(Description = "DotLiquid")]
        public DotLiquid.Template TestDotLiquid()
        {
            return DotLiquid.Template.Parse(TextTemplateDotLiquid);
        }

        [Benchmark(Description = "Stubble")]
        public Stubble.Core.Tokens.MustacheTemplate TestStubble()
        {
            return new Stubble.Core.Settings.RendererSettingsBuilder().BuildSettings().Parser.Parse(TextTemplateMustache);
        }

        [Benchmark(Description = "Nustache")]
        public Nustache.Core.Template TestNustache()
        {
            var template = new Nustache.Core.Template();
            template.Load(new StringReader(TextTemplateMustache));
            return template;
        }

        [Benchmark(Description = "Handlebars.NET")]
        public HandlebarsTemplate<object, object> TestHandlebars()
        {
            return HandlebarsDotNet.Handlebars.Compile(TextTemplateMustache);
        }

        [Benchmark(Description = "Cottle")]
        public Cottle.Documents.SimpleDocument TestCottle()
        {
            return new Cottle.Documents.SimpleDocument(TextTemplateCottle);
        }

        [Benchmark(Description = "Fluid")]
        public Fluid.IFluidTemplate TestFluid()
        {
            static void ThrowError()
            {
                throw new InvalidOperationException("Fluid template not parsed");
            }

            if (!Fluid.FluidParserExtensions.TryParse(FluidParser, TextTemplateDotLiquid, out var template))
            {
                ThrowError();
            }
            return template;
        }
    }

    /// <summary>
    /// A benchmark for template renderers
    /// </summary>
    [MemoryDiagnoser]
    [HtmlExporter]
    [RPlotExporter]
    public class BenchRenderers
    {
        private readonly Scriban.Template _scribanTemplate;
        private readonly DotLiquid.Template _dotLiquidTemplate;
        private readonly Stubble.Core.Settings.RendererSettings _stubbleSettings;
        private readonly Stubble.Core.Tokens.MustacheTemplate _stubbleTemplate;
        private readonly Nustache.Core.Template _nustacheTemplate;
        private readonly HandlebarsTemplate<object, object> _handlebarsTemplate;
        private readonly Cottle.Documents.SimpleDocument _cottleTemplate;
        private readonly Fluid.IFluidTemplate _fluidTemplate;

        private const string Lorem = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum";

        private readonly List<Product> _products;
        private readonly List<ScriptObject> _scribanProducts;
        private readonly List<DotLiquid.Hash> _dotLiquidProducts;
        private readonly Cottle.Value _cottleProducts;

        private readonly Dictionary<Cottle.Value, Cottle.Value> _cottleStringStore;

        private readonly TemplateContext _templateContext;

        public BenchRenderers()
        {
            var parsers = new BenchParsers();
            _scribanTemplate = parsers.TestScriban();
            _dotLiquidTemplate = parsers.TestDotLiquid();
            _stubbleTemplate = parsers.TestStubble();
            _stubbleSettings = new Stubble.Core.Settings.RendererSettingsBuilder().BuildSettings();
            _nustacheTemplate = parsers.TestNustache();
            _handlebarsTemplate = parsers.TestHandlebars();
            _cottleTemplate = parsers.TestCottle();
            _fluidTemplate = parsers.TestFluid();

            const int ProductCount = 500;
            _products = new List<Product>(ProductCount);
            _scribanProducts = new List<ScriptObject>();
            _dotLiquidProducts = new List<DotLiquid.Hash>(ProductCount);
            var cottleProducts = new List<Cottle.Value>();

            for (int i = 0; i < ProductCount; i++)
            {
                var product = new Product("Name" + i, i, Lorem);
                _products.Add(product);

                var hash = new Hash() { ["name"] = product.Name, ["price"] = product.Price, ["description"] = product.Description };
                _dotLiquidProducts.Add(hash);

                var obj = new ScriptObject { ["name"] = product.Name, ["price"] = product.Price, ["description"] = product.Description };
                _scribanProducts.Add(obj);

                var value = new Dictionary<Cottle.Value, Cottle.Value> { ["name"] = product.Name, ["price"] = product.Price, ["description"] = product.Description };
                cottleProducts.Add(value);
            }

            _cottleProducts = cottleProducts;

            _templateContext = new TemplateContext();
            _templateContext.BuiltinObject["truncate"] = ((ScriptObject)_templateContext.BuiltinObject["string"])["truncate"];

            // For Cottle, we match the behavior of Scriban that is accessing the Truncate function via an reflection invoke
            // In Scriban, we could also have a direct Truncate function, but it is much less practical in terms of declaration
            _cottleStringStore = new Dictionary<Cottle.Value, Cottle.Value>();
            _cottleStringStore["truncate"] = new Cottle.Functions.NativeFunction(values => StringFunctions.Truncate(values[0].AsString, Convert.ToInt32(values[1].AsNumber)), 2);
        }

        [Benchmark(Description = "Scriban")]
        public string TestScriban()
        {
            // We could use the following simpler version, but we demonstrate the use of PushGlobal/PopGlobal object context
            // for a slightly higher efficiency and the reuse of a TemplateContext on the same thread
            //return _scribanTemplate.Render(new { products = _dotLiquidProducts });
            _templateContext.BuiltinObject.SetValue("products", _scribanProducts, false);
            _templateContext.PushOutput(StringBuilderOutput.GetThreadInstance());
            var result = _scribanTemplate.Render(_templateContext);
            _templateContext.PopOutput();
            return result;
        }

        [Benchmark(Description = "ScribanAsync")]
        public async ValueTask<string> TestScribanAsync()
        {
            // We could use the following simpler version, but we demonstrate the use of PushGlobal/PopGlobal object context
            // for a slightly higher efficiency and the reuse of a TemplateContext on the same thread
            //return _scribanTemplate.Render(new { products = _dotLiquidProducts });
            var obj = new ScriptObject { { "products", _scribanProducts } };
            _templateContext.PushGlobal(obj);
            var result = await _scribanTemplate.RenderAsync(_templateContext);
            _templateContext.PopGlobal();
            return result;
        }

        [Benchmark(Description = "DotLiquid")]
        public string TestDotLiquid()
        {
            // DotLiquid forces to rework the original List<Product> into a custom object, which is not the same behavior as Scriban (easier somewhat because no late binding)
            return _dotLiquidTemplate.Render(DotLiquid.Hash.FromAnonymousObject(new { products = _dotLiquidProducts }));
        }

        [Benchmark(Description = "Stubble")]
        public string TestStubble()
        {
            var renderer = new Stubble.Core.StubbleVisitorRenderer();
            var props = new Dictionary<string, object> { ["products"] = _dotLiquidProducts };
            int i = 0;
            props["truncate"] = new Func<string, object>((str) => Scriban.Functions.StringFunctions.Truncate(renderer.Render(str, _dotLiquidProducts[i++]), 15));
            return renderer.Render(BenchParsers.TextTemplateMustache, props);
        }

        [Benchmark(Description = "Nustache")]
        public string TestNustache()
        {
            int i = 0;
            return Nustache.Core.Render.StringToString(BenchParsers.TextTemplateMustache, new
            {
                products = _dotLiquidProducts,
                truncate = new Func<string, object>((str) => Scriban.Functions.StringFunctions.Truncate(Nustache.Core.Render.StringToString(str, _dotLiquidProducts[i++]), 15))
            });
        }

        [Benchmark(Description = "Handlebars")]
        public string TestHandlebars()
        {
            return _handlebarsTemplate(new
            {
                products = _dotLiquidProducts
            });
        }

        [Benchmark(Description = "Cottle")]
        public string TestCottle()
        {
            // This is done to match the behavior of Scriban (no preparation of the datas)
            return _cottleTemplate.Render(Cottle.Context.CreateBuiltin(new Dictionary<Cottle.Value, Cottle.Value>
            {
                ["string"] = _cottleStringStore,
                ["products"] = _cottleProducts
            }));
        }

        [Benchmark(Description = "Fluid")]
        public string TestFluid()
        {
            var templateContext = new Fluid.TemplateContext();
            templateContext.SetValue("products", Fluid.Values.FluidValue.Create(_products, templateContext.Options));
            // DotLiquid forces to rework the original List<Product> into a custom object, which is not the same behavior as Scriban (easier somewhat because no late binding)
            return Fluid.FluidTemplateExtensions.Render(_fluidTemplate, templateContext);
        }

        public class Product
        {
            public Product(string name, float price, string description)
            {
                Name = name;
                Price = price;
                Description = description;
            }

            public string Name { get; set; }

            public float Price { get; set; }

            public string Description { get; set; }
        }
    }
}
