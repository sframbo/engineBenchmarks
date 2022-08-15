using AutoFixture;
using AutoFixture.AutoMoq;
using BenchmarkDotNet.Attributes;

using HandlebarsDotNet;
using Scriban;

namespace ConsoleApp3
{
    [MemoryDiagnoser]
    [HtmlExporter]
    [RPlotExporter]
    public class EngineBenchmarks
    {
        public IFixture fixture =  new Fixture().Customize(new AutoMoqCustomization());
        public object Context;
        public string templateString 
            = @"info1: {{info1}} info2: {{info2}} info3: {{info3}} info4: {{info4}}";

        HandlebarsTemplate<object, object>? hbTemp;
        Template? scTemp; 

        public EngineBenchmarks()
        {
            Context = new
            {
                info1 = fixture.Create<string>(),
                info2 = fixture.Create<string>(),
                info3 = fixture.Create<string>(),
                info4 = fixture.Create<string>(),
            };

            hbTemp = Handlebars.Compile(templateString);
            scTemp = Template.Parse(templateString);
        }

        [Benchmark(Baseline = true)]
        public void Handlebar_Compile()
        {
            Handlebars.Compile(templateString);
        }

        [Benchmark]
        public void Handlebar_Render()
        {
            hbTemp(Context);
        }

        [Benchmark]
        public void Scriban_Compile()
        {
            Template.Parse(templateString);
        }

        [Benchmark]
        public void Scriban_Render()
        {
            scTemp.Render(Context);
        }
    }
}
