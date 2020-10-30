﻿using AngleSharp.Dom;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Smart.Parser.Lib.Adapters.HtmlSchemes
{
    internal class ArbitrationCourt2 : ArbitrationCourt1
    {
        public override bool CanProcess(IDocument document)
        {
            try
            {
                var selection = document.QuerySelectorAll("div.b-card-header-container");

                return selection.Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override IEnumerable<IElement> GetMembers(string name, string year)
        {
            IElement tableElement;
            if (year != null)
            {
                tableElement = this
                    .Document
                    .All
                    .First(x => x.LocalName == "div" && x.Attributes.Any(y => y.Name == "rel" && y.Value == year));
            }
            else
            {
                tableElement = this.Document.QuerySelectorAll("div.b-card-income-container").First();
            }

            var members = tableElement.Children;
            return members;
        }
    }
}