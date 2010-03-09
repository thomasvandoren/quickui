﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if DEBUG
using NUnit.Framework;
#endif

namespace qc
{
    /// <summary>
    /// An HTML fragment found in Quick markup.
    /// </summary>
    public class HtmlElement : MarkupElement
    {
        public string Html { get; set; }
        public MarkupNodeCollection ChildNodes { get; set; }

        public HtmlElement()
        {
        }

        public HtmlElement(string html)
        {
            this.Html = html;
        }

        public HtmlElement(string html, string id) : this(html)
        {
            this.Id = id;
        }

        public HtmlElement(IEnumerable<MarkupNode> items)
        {
            this.ChildNodes = new MarkupNodeCollection(items);
        }

        /// <summary>
        /// Return the JavaScript for the given HTML node.
        /// </summary>
        public override string EmitJavaScript(int indentLevel)
        {
            string html = EscapeJavaScript(Html);

            if (Id == null && ChildNodes == null)
            {
                // Simplest case; just quote the HTML and return it.
                return Template.Format(
                    "{Html}",
                    new {
                        Html = html
                    });
            }

            return Template.Format(
                "{VariableDeclaration}$({Html}){ChildNodes}[0]",
                new
                {
                    VariableDeclaration = EmitVariableDeclaration(),
                    Html = html,
                    ChildNodes = EmitChildren(indentLevel)
                });
        }

        private string EmitChildren(int indentLevel)
        {
            return (ChildNodes == null)
                ? String.Empty
                : Template.Format(
                    ".items(\n{ChildNodes}{Tabs})",
                    new
                    {
                        ChildNodes = ChildNodes.EmitItems(indentLevel + 1),
                        Tabs = Tabs(indentLevel)
                    });
        }

#if DEBUG
        [TestFixture]
        public new class Tests
        {
            [Test]
            public void Text()
            {
                HtmlElement node = new HtmlElement("Hello");
                Assert.AreEqual("\"Hello\"", node.EmitJavaScript());
            }

            [TestCase("<div>Hi</div>", Result = "\"<div>Hi</div>\"")]
            [TestCase("<div><h1/><p>Hello</p></div>", Result = "\"<div><h1/><p>Hello</p></div>\"")]
            public string Html(string source)
            {
                HtmlElement node = new HtmlElement(source);
                return node.EmitJavaScript();
            }

            [Test]
            public void HtmlWithId()
            {
                HtmlElement node = new HtmlElement("<div id=\"foo\">Hi</div>", "foo");
                Assert.AreEqual("this.foo = $(\"<div id=\\\"foo\\\">Hi</div>\")[0]", node.EmitJavaScript());
            }

            [Test]
            public void HtmlContainsHtmlWithId()
            {
                // <div><h1/><p id="content">Hello</p></div>
                HtmlElement node = new HtmlElement("<div />")
                {
                    ChildNodes = new MarkupNodeCollection(new MarkupNode[] {
                        new HtmlElement("<h1 />"),
                        new HtmlElement("<p id=\"content\">Hello</p>", "content")
                    })
                };
                Assert.AreEqual(
                    "$(\"<div />\").items(\n" +
                    "\t\"<h1 />\",\n" +
                    "\tthis.content = $(\"<p id=\\\"content\\\">Hello</p>\")[0]\n" +
                    ")[0]",
                    node.EmitJavaScript());
            }
        }
#endif
    }
}