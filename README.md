# Project Description 
**X2ml** (*X to ML*) is meant to ease and speed-up the emission of markup code (such as HTML) in your C# projects. 
Using **X2ml** you emit XML code combining strings and operators. It even supports templating and data-binding through lambda expressions.

# Simple usage

```cs
var x = X.X2ml;
var x2ml = x["html"] / "body" / "h1" * "Hello X!";
var code = x2ml.ToXmlString();
```

# Advanced usages 

1. You can blend **X2ml** and **Linq** to build an XHTML code filled with data

```cs
var tableInRam = new Dictionary<string, decimal>
                 {
                     {"Jan", 1000M},
                     {"Feb", 2000M},
                     {"Mar", 3000M}
                 };
//Building documents from Linq expressions is easy! 
var x = X.X2ml; //X is the magic class
var q = x["html"]/"body"/"table" //This looks like a breadcrumb
                            * "@align=center" /
                        from row in tableInRam
                        select x["tr"] //This syntax is shorter than new X("tr"), which is allowed too
                                  / "th" * row.Key //The slash '/'operator creates a child; it works with IEnumerable<X> too
                                  % "td" * row.Value.ToString(); //The percentage '%' operator creates siblings 
var xml = q.ToXmlString(); //This one builds the XML fragment 
//Compare the previous code with this equivalent Linq2Xml snippet:
var xe = new XElement("html",
                            new XElement("body",
                                new XElement("table",
                                    new XAttribute("align", "center"),
                                    from row in tableInRam
                                    select new XElement("tr",
                                           new XElement("th", row.Key),
                                           new XElement("td", row.Value.ToString())))));
var xml2 = xe.ToString();
```

2. You can bind an **X2ml template** to a **data source** and use **lambda expressions** to declare dependencies.  

```cs
var tableInRam = new Dictionary<string, decimal> 
                 { 
                     {"Jan", 1000M}, 
                     {"Feb", 2000M}, 
                     {"Mar", 3000M} 
                 };
var x = X.X2ml; 
var template = x["html"] / "body" 
                           / "table"  * "@align=center" 
                             / "tr" * X.BindTo(tableInRam) // X.BindTo(IEnumerable<T>) binds a container element to a data source 
                               / "th" * ((e,i) => String.Format("#{0}, {1}", i+1, e.Key)) // You can use a Func<T,int,object> to declare a binding dependency
                               % "td" * (e => e.Value); //If you don't need the item's index, you can use a Func<T,object> to set a binding dependency
var xml = q.ToXmlString(); //This one builds the following XML fragment
```

```xml
<html>
  <body>
    <table align='center'>
      <tr><th>#1, Jan</th><td>1000</td></tr>
      <tr><th>#2, Feb</th><td>2000</td></tr>
      <tr><th>#3, Mar</th><td>3000</td></tr>
    </table>
  </body>
</html>
```

3. You can build a **template**, bind it to a **data source** and get a collection of **XML fragments** filled with proper data:

```cs
var x = X.X2ml;
var chars = new[] { '0', '1' };  // the data source

var template = x["A"] * X.BindTo(chars)
                / "B" * (c => c.ToString() + "|" + c.ToString()) 
                % "C" * (c => "fixed")
                % "D" * "const"; 
IEnumerable<X> xes = template.DataRoot.BindAndSpawn();
```

In the previous code, **xes** is a collection of standard X objects. For instance **xes.First().ToXmlString()** returns the following XML fragment:

```xml
<A>
 <B>0|0</B>
 <C>fixed</C>
 <D>const</D>
</A>
```

4. You can occasionally find more about **X2ml** on this [blog](http://www.h3mm3.com/search/label/X2ml).

`Last edited Dec 6, 2011 at 10:54 PM by Hemme, version 28 [Codeplex]`
