using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace X2ml.Tests
{
    [TestClass]
    public class X2mlTest
    {


        [TestMethod]
        public void Only_Root()
        {
            var x = X.X2ml;
            var x2ml = x["root"];
            string xml = CastAndString(x2ml);
            Assert.AreEqual("<root />", xml);
        }

        private string CastAndString(X x)
        {
            var xe = (XElement)x;
            return xe.ToString();
        }

        [TestMethod]
        public void One_Child()
        {
            var x = X.X2ml;
            var x2ml = x["root"] / "child";
            var xml = CastAndString(x2ml);
            Assert.AreEqual(XElement.Parse("<root><child /></root>").ToString(), xml);
        }

        [TestMethod]
        public void Two_Children()
        {
            var x = X.X2ml;
            var x2ml = x["root"]
                    / "child1"  //Slash creates a child
                    % "child2"; //Percentage creates a sibling

            var xml = CastAndString(x2ml);
            Assert.AreEqual(XElement.Parse("<root><child1 /><child2 /></root>").ToString(), xml);
        }

        [TestMethod]
        public void A_Child_Has_One_Attribute()
        {
            var x = X.X2ml;
            var x2ml = x["html"]
                    / "body"
                        / "div"
                            * "@style=margin:0"; //asterisk creates an attribute if first char is "@"... (continues to the next test)
            var xml = CastAndString(x2ml);
            Assert.AreEqual(XElement.Parse("<html><body><div style='margin:0'/></body></html>").ToString(), xml);
        }

        [TestMethod]
        public void Several_Children_And_Attributes()
        {
            var x = X.Element("html")
                    / "body"
                        / "div"
                                * "@style=margin:0"
                                * "@id=main"
                            / "h1" * "Hello X!" //...otherwise it creates an element value
                            % "p" * "This html file has been generated with X.";
            var xml = CastAndString(x);
            Assert.AreEqual(XElement.Parse(@"
<html>
    <body>
        <div style='margin:0' id='main'>
            <h1>Hello X!</h1>
            <p>This html file has been generated with X.</p>
        </div>
    </body>
</html>").ToString(), xml);

        }

        [TestMethod]
        public void Using_Linq_With_X()
        {
            var tableInRam = new Dictionary<string, decimal>
                                 {
                                     {"Jan", 1000M},
                                     {"Feb", 2000M},
                                     {"Mar", 3000M}
                                 };
            var x = X.X2ml;
            for (int i = 0; i < 5; i++)
            {
                var w = new Stopwatch();
                w.Start();
                var q = x["html"] / "body" / "table" * "@align=center" /   //Building documents from linq expressions is easy!
                        from row in tableInRam
                        select x["tr"] //this syntax is shorter than new X("tr")
                               / "th" * row.Key
                               % "td" * row.Value.ToString();
                
                var xml = q.ToXmlString();
                w.Stop();

                var delta = w.Elapsed;

                //------------
                w = new Stopwatch();
                w.Start();
                var xe = new XElement("html",
                            new XElement("body",
                                new XElement("table",
                                    new XAttribute("align", "center"),
                                    from row in tableInRam
                                    select new XElement("tr",
                                            new XElement("th", row.Key),
                                            new XElement("td", row.Value.ToString())))));
                var xml2 = xe.ToString();
                w.Stop();

                var delta2 = w.Elapsed;

                //if (i == 4) Assert.Fail(delta + " | " + delta2); //Uncomment to compare performance with standard XElement usage ^_^

                Assert.AreEqual(
                    XElement.Parse(
                        @"
<html>
    <body>
        <table align='center'>
            <tr><th>Jan</th><td>1000</td></tr>
            <tr><th>Feb</th><td>2000</td></tr>
            <tr><th>Mar</th><td>3000</td></tr>
        </table>
    </body>
</html>")
                        .ToString(), XElement.Parse(xml).ToString());
            }
        }

        [TestMethod]
        public void Mixed_Text_And_Markup()
        {
            var x = X.X2ml;
            var x2ml = x["r"] / new[] { 
                        x["a"] * "A"
                              / "b" * "B", //I have no more operators to overload, so to go up one level I have to use a collection initializer
                        x["c"]};
            Assert.AreEqual("<r><a>A<b>B</b></a><c/></r>", x2ml.ToXmlString());
        }
    }

}
