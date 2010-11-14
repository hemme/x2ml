using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace X2ml.Tests
{
    [TestClass]
    public class XBoundTest
    {
        [TestMethod]
        public void Elements_After_BindAndSpawn_Have_Right_Structure()
        {
            var x = X.X2ml;
            var chars = new[] { '*', '*' };

            var x2ml = x["A"] * X.BindTo(chars)
                       / "B" * (c => c.ToString())
                       % "C" * (c => c.ToString())
                       % "D" * "@e"*(c => String.Empty); //just testing structure, value will appear in breadcroumb if not null

            foreach (var k in x2ml.DataRoot.BindAndSpawn())
            {
                Assert.AreEqual("/A", k.BreadCrumb);
                Assert.AreEqual("/A/B", k.Children[0].BreadCrumb);
                Assert.AreEqual("/A/C", k.Children[1].BreadCrumb);
                Assert.AreEqual("/A/D[e=]", k.Children[2].BreadCrumb);
            }
        }

        [TestMethod]
        public void Elements_After_BindAndSpawn_Contains_Right_Data()
        {
            var x = X.X2ml;
            var chars = new[] { '0', '1' };

            var x2ml = x["A"] * X.BindTo(chars)
                           / "B"
                                 * (c => c.ToString() + "|" + c.ToString())     // <B>[0|0 oppire 1|1]</B>
                           % "C" * (c => "fixed")                               // <C>fixed</C>
                           % "D" * "const";                                     // <D>const</D>
            var bound = x2ml.DataRoot.BindAndSpawn().ToList();
            for (var i = 0; i < chars.Length; i++)
                Assert.AreEqual("<A><B>" + chars[i] + "|" + chars[i] + "</B><C>fixed</C><D>const</D></A>", bound[i].ToXmlString());
        }

        [TestMethod]
        public void DeepBinding()
        {
            var x = X.X2ml;
            var chars = new[] { '?', '!' };

            var x2ml = x["a"] * X.BindTo(chars)
                           / "b" * (c => c.ToString())
                               / "c" * (c => "c" + c)
                               % "d" * (c => "d" + c);
            var bound = x2ml.DataRoot.BindAndSpawn().ToList()[1];
            Assert.AreEqual("<a><b>!<c>c!</c><d>d!</d></b></a>", bound.ToXmlString());
        }
        [TestMethod]
        public void BindAndSpawn_With_No_Data_Returns_Empty_Collection()
        {
            var x = X.X2ml;

            var empty = new List<char>();
            var x2ml = x["tr"] * X.BindTo(empty)
                         / "td" * (c => c.ToString());
            Assert.IsTrue(x2ml.BindAndSpawn().Count() == 0);
        }

        [TestMethod]
        public void DataRoot_Returns_XBound_Object()
        {
            var x = X.X2ml;
            var chars = new[] { 'a', 'b', 'c' };

            var x2ml = x["tr"] * X.BindTo(chars)
                         / "td" * (c => c.ToString());

            var root = x2ml.DataRoot;
            Assert.IsTrue(root.Name == "tr");
        }

        [TestMethod]
        public void BindAndSpawn_With_N_Data_Returns_Collection_With_N_Elements()
        {
            var x = X.X2ml;
            var chars = new[] { 'a', 'b', 'c' };

            var x2ml = x["tr"] * X.BindTo(chars)
                         / "td" * (c => c.ToString());
            Assert.IsTrue(x2ml.DataRoot.BindAndSpawn().Count() == chars.Length);
        }

        [TestMethod]
        public void Collection_Binding()
        {
            var x = X.X2ml;
            var elements = new[] { new { Name = "foo", Value = "bar" }, new { Name = "baz", Value = "boom" } };
            var x2ml = x["body"] / "table" / "tr" * X.BindTo(elements)
                                                / "td" * (e => e.Name)
                                                % "td" * "&nbsp;"
                                                % "td" * (e => e.Value);
            Assert.AreEqual("<body><table><tr><td>foo</td><td>&nbsp;</td><td>bar</td></tr><tr><td>baz</td><td>&nbsp;</td><td>boom</td></tr></table></body>",
                x2ml.ToXmlString());
        }

        [TestMethod]
        public void Usage_Samples_1()
        {
            var tableInRam = new Dictionary<string, decimal> {{"Jan", 1000M}, {"Feb", 2000M}, {"Mar", 3000M}};
            var x = X.X2ml;
            var q1 = x["html"]/"body"/"table"*"@align=center"/"tr"*X.BindTo(tableInRam)
                    /"th"*(e => e.Key) 
                    %"td"*(e => e.Value.ToString());  

            var q2 = x["html"] / "body" / "table" * "@align=center" /   
                    from row in tableInRam
                    select x["tr"] 
                           / "th" * row.Key
                           % "td" * row.Value.ToString();

            Assert.AreEqual(q1.ToXmlString(),q2.ToXmlString());
        }

        [TestMethod]
        public void Enabling_Func_T_object_in_binding_expressions()
        {
            var names = new[] { "foo", "foobar" };
            var x = X.X2ml;
            var x2ml = x["words"] / "length" * X.BindTo(names) * (k => k.Length);
            Assert.AreEqual("<words><length>3</length><length>6</length></words>",x2ml.ToXmlString());
        }
    }
}
