using System;
using Carrot.Configuration;
using Yoox.Backend.Messages.Article;
using Yoox.Backend.Messages.Common;

namespace Carrot.BasicSample
{
    [MessageBinding("urn:message:foo")]
    public class Foo
    {
        public Int32 Bar { get; set; }
    }

    public class FooArticle : ICurrentArticleVersionChanged
    {
        public FooArticle()
        {
            MicroCategory = new LookUpProperty();
            MacroCategory = new LookUpProperty();
            Brand = new LookUpProperty();
            SizeClass = new LookUpProperty();
            SaleLine = new LookUpProperty();
            Color = new LookUpProperty();
            MacroColor = new LookUpProperty();
        }

        public string Code10 { get; set; }
        public string Code8 { get; set; }
        public string ParentCode10 { get; set; }
        public string Description { get; set; }
        public string Gender { get; set; }
        public string DivisionCode { get; set; }
        public bool Active { get; set; }
        public string Season { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastChangeDate { get; set; }
        public ILookUpProperty MicroCategory { get; set; }
        public ILookUpProperty MacroCategory { get; set; }
        public ILookUpProperty Brand { get; set; }
        public ILookUpProperty SizeClass { get; set; }
        public ILookUpProperty SaleLine { get; set; }
        public ILookUpProperty Color { get; set; }
        public ILookUpProperty MacroColor { get; set; }
        public string MadeIn { get; set; }
        public DateTimeOffset OccurredOn { get; set; }
    }

    public class LookUpProperty : ILookUpProperty
    {
        public int Key { get; set; }
        public string Value { get; set; }
    }
}