using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace X2ml
{
    public partial  class X
    {
        private XElement ToXElement()
        {
            return new XElement(Name,
                                from c in Children
                                select (c.Type == XType.InnerText ?
                                    (XNode)new XText(c.Value) : (XNode)c.ToXElement()),
                                from a in Attributes
                                select new XAttribute(a.Name, a.Value));
        }

        public override string ToString()
        {
            return BreadCrumb;
        }

        public string ToXmlString()
        {
            var sb = new StringBuilder();
            Root.ToStringBuilder(sb);
            return sb.ToString();          
        }

        protected virtual void ToStringBuilder(StringBuilder sb)
        {
            if (Type == XType.InnerText)
            {
                sb.Append(Value);
                return;
            }

            sb.Append('<');
            sb.Append(Name);
            if (Attributes.Count > 0)
                foreach (var a in Attributes)
                    sb.Append(
                        string.Format(" {0}='{1}'",
                                      a.Name, a.Value));
            if (Children.Count == 0)
                sb.Append("/>");
            else
            {
                sb.Append('>');
                foreach (var c in Children)
                    c.ToStringBuilder(sb);
                sb.Append("</");
                sb.Append(Name);
                sb.Append('>');
            }
        }

        /// <summary>
        /// Converts an X to a System.Xml.Linq.XElement.
        /// </summary>
        public static explicit operator XElement(X x)
        {
            X r = x.Root;
            return r.ToXElement();
        }

        public string BreadCrumb
        {
            get
            {
                return (Parent != null ? Parent.BreadCrumb : "") + "/" + Name;
            }
        }
        public X Root
        {
            get
            {
                if (Parent != null) return Parent.Root;
                return this;
            }
        }


        private X Parent { get; set; }
        private XType Type { get; set; }
        public string Name { get; set; }
        public List<X> Children { get; set; }
        public List<X> Attributes { get; set; }
        public string Value { get; set; }
        //protected bool _isFirstChild;

        public X()
        {
            Children = new List<X>();
            Attributes = new List<X>();
        }

        public X(string name)
            : this()
        {
            Name = name;
            Type = XType.Element;
        }

        public static X InnerText(string value)
        {
            var e = new X(null);
            e.Type = XType.InnerText;
            e.Value = value;
            return e;
        }

        public static X Element(string name)
        {
            var e = new X(name);
            return e;
        }

        public static X Attribute(string name)
        {
            var pos = name.IndexOf('=');
            X a = (pos != -1)
                      ? Attribute(name.Substring(0, pos), name.Substring(pos + 1))
                      : Attribute(name, null);

            return a;
        }

        public static X Attribute(string name, string value)
        {
            var a = new X { Name = name };
            a.Type = XType.Attribute;
            a.Value = value;
            return a;
        }

        public X Add(X content)
        {
            System.Diagnostics.Debug.Assert(Type != XType.Attribute);
            content.Parent = this;
            switch (content.Type)
            {
                case XType.InnerText:
                case XType.Element:
                    Children.Add(content);
                    break;
                default: //attribute
                    Attributes.Add(content);
                    break;
            }

            return content;
        }

        public X Add(IEnumerable<X> content)
        {
            OnAddChildren(content);
            OnAddAttributes(content);
            return this;
        }

        private void OnAddChildren(IEnumerable<X> content)
        {
            var adding = content.Where(x => x.Type == XType.Element||x.Type==XType.InnerText).Select(x => x.Root).ToArray();
            adding.Any(a =>
            {
                a.Parent = this;
                return false;
            });

            Children.AddRange(adding);
            //adding.First()._isFirstChild = true;
        }

        private void OnAddAttributes(IEnumerable<X> content)
        {
            var adding = content.Where(x => x.Type == XType.Attribute);
            adding.Any(a =>
            {
                a.Parent = this;
                return false;
            });
            Attributes.AddRange(adding);
        }

        private X ParentElementOrSelf
        {
            get { return Type == XType.Element || Parent == null ? this : Parent.ParentElementOrSelf; }
        }

        private X AddAttribute(X a)
        {
            X p = this.Type == XType.Attribute ? Parent : this;
            a.Parent = p;
            p.Add(a);
            return a;
        }


        ////buggy.... must investigate
        //public static X operator <<(X x, int levelsUp)
        //{
        //    if (x.Type == XType.Attribute) throw new InvalidOperationException("Cannot bubble attributes.");

        //    while (--levelsUp > 0)
        //        x.Bubble();

        //    return x;
        //}

        //private void Bubble()
        //{
        //    X p = this;
        //    if (p.Parent != null)
        //        p = p.Parent.ParentElementOrSelf;

        //    if (p == null) return;

        //    this.Parent.Children.Remove(this);
        //    p.Add(this);
        //}
        
        public static X2 X2ml { get { return X2.Instance;  } }

        public static XBound<T> BindTo<T>(IEnumerable<T> data)
        {
            return new XBound<T>(data);
        }

        public IEnumerable<X> Siblings()
        {
            return from s in Parent.Children
                   where s != this
                   select s;
        }

        #region Operators

        public static X operator /(X x, string child)
        {
            var n = X.Element(child);
            //n._isFirstChild = true;

            X p = x.ParentElementOrSelf;//x.Type == XType.Element ? x : x.Parent;
            n.Parent = p;
            p.Add(n);
            return n;
        }

        public static X operator /(X x, IEnumerable<X> xs)
        {
            x.ParentElementOrSelf.OnAddChildren(xs);
            return x;
        }

        public static X operator *(X x, string content)
        {
            if (String.IsNullOrEmpty(content)) return x;

            if (content[0] == '@') //adding attribute
            {
                return x.AddAttribute(X.Attribute(content.Substring(1)));
            }

            //adding element innerText

            var it = X.InnerText(content);
            x.ParentElementOrSelf.Add(it);
            return x;
        }

        public static X operator %(X x, string element)
        {
            var e = X.Element(element);
            var p = x.Type == XType.Element ? x.Parent : x;

            e.Parent = p;
            p.Add(e);

            return e;
        }
       
        #endregion

        #region Nested Types

        private enum XType
        {
            Element = 0,
            Attribute = 1,
            InnerText = 2
        }

        public sealed class X2
        {
            private X2() { }

            public X this[String name]
            {
                get { return new X(name); }
            }

            

            private static X2 _x2;
            public static X2 Instance
            {
                get { return _x2 ?? (_x2 = new X2()); }
            }

        }

        public sealed class XBound<T> : X
        {
            private Func<T, string> _selector;
            private readonly IEnumerable<T> _data;

            internal XBound(X x) : this (x, null) { }
            
            internal XBound(X x, IEnumerable<T> data):this(data)
            {
                base.Name = x.Name;
                base.Parent = x.Parent;
                base.Value = x.Value;
                base.Type = x.Type;
                base.Attributes.AddRange(x.Attributes);
                OnSubstitute(x);

                base.Children.AddRange(x.Children);
            }

            private void OnSubstitute(X x)
            {
                if (x.Parent!=null)
                {
                    var pos = x.Parent.Children.IndexOf(x);
                    x.Parent.Children.Insert(pos,this);

                    x.Parent.Children.Remove(x);
                    x.Parent = null;
                }
            }

            internal XBound(IEnumerable<T> data)
            {
                _data = data;
            }

            internal XBound(Func<T, string> selector)
            {
                _selector = selector;
            }

            #region Operators

            public static XBound<T> operator / (XBound<T> b, string s)
            {
                return new XBound<T>((X)b / s);
            }

            public static XBound<T> operator % (XBound<T> b, string s)
            {
                return new XBound<T>((X)b % s);
            }

            public static XBound<T> operator * (XBound<T> b, string s)
            {
                var x = (X)b * s;
                return new XBound<T>(x);
            }

            public static XBound<T> operator * (XBound<T> b, Func<T, string> selector)
            {
                return b * new XBound<T>(selector);
            }

            public static XBound<T> operator * (X x, XBound<T> placeholder)
            {
                var b = new XBound<T>(x, placeholder._data);
                b._selector = placeholder._selector;

                return b;
            }
            #endregion

            private IEnumerable<T> FetchData(out XBound<T> owner)
            {
                XBound<T> p = this;
                while (p!=null && p._data==null)
                {
                    p = p.Parent as XBound<T>;
                }
                return (owner=(p ?? this))._data;
            }

            public XBound<T> DataRoot
            {
                get
                {
                    var dr = this;
                    while (dr != null && dr._data == null)
                        dr = dr.Parent as XBound<T>;

                    return dr;
                }
            }

            public IEnumerable<X> BindAndSpawn()
            {
                XBound<T> owner;
                var data = FetchData(out owner);
                return BindAndSpawn(owner, data);
            }

            private IEnumerable<X> BindAndSpawn(XBound<T> dataowner, IEnumerable<T> data)
            {
                /*
                  "tr" * X.BindTo(elements)         "1","2"
                      / "td" * (e => e)       <tr><td>1</td><td>&nbsp;</td><td>1</td></tr>
                      % "td" * "&nbsp;"       <tr><td>2</td><td>&nbsp;</td><td>2</td></tr>
                      % "td" * (e => e);
                */
                var dbc = new List<X>();
                XBound<T> cb;
                foreach (T d in data)
                {
                    var boundItems = new List<X>();
                    foreach (X xc in Children)
                    {
                        cb = xc as XBound<T>;
                        if (cb != null)
                        {
                            var value = (cb._selector != null ? cb._selector(d) : cb.Value != null ? cb.Value : null);
                            var x = (value != null) ? new X(cb.Name) * value : new X(cb.Name);

                            var cb2 = cb.BindAndSpawn(dataowner, data);
                            x.OnAddChildren(cb2);
                            x.OnAddAttributes(cb2);

                            boundItems.Add(x);
                        }
                        else
                        {
                            boundItems.Add(xc);
                        }
                    }
                    var newBorn = new X(Name);
                    var b = newBorn * Value;
                    newBorn.OnAddChildren(boundItems);
                    dbc.Add(newBorn);
                }
                return dbc;
            }

            protected override void ToStringBuilder(StringBuilder sb)
            {
                if (_data != null)
                {
                    foreach (var x in BindAndSpawn())
                        x.ToStringBuilder(sb);
                }
                else base.ToStringBuilder(sb);
            }
        }

        #endregion
    } 
    
}

