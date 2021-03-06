﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace X2ml
{
    public partial class X
    {
        public sealed class XBound<T> : X
        {
            private Func<T, int, object> _selector;
            private IEnumerable<T> _data;

            /// <summary>
            /// Creates a new XBound shallow-copying each property of x and replaces the new instance to x in its parent-child chain
            /// </summary>
            /// <param name="x">X object to be replaced</param>
            internal XBound(X x) : this(x, null) { }

            /// <summary>
            /// Creates a new XBound shallow-copying each property of x and replaces the new instance to x in its parent-child chain
            /// </summary>
            /// <param name="x">X object to be replaced</param>
            /// <param name="data">Data source</param>
            internal XBound(X x, IEnumerable<T> data)
                : this(data)
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
                if (x.Parent != null)
                {
                    var pos = x.Parent.Children.IndexOf(x);
                    if (pos > -1)
                    {
                        x.Parent.Children.Insert(pos, this);
                        x.Parent.Children.Remove(x);
                        x.Parent = null;
                    }
                }
            }

            internal XBound(IEnumerable<T> data)
            {
                _data = data;
            }

            internal XBound(Func<T, string> selector)
            {
                _selector = (t,i)=>selector(t);
            }

            internal XBound(Func<T, int, object> selector)
            {
                _selector = selector;
            }

            #region Operators

            public static XBound<T> operator /(XBound<T> b, string s)
            {
                return new XBound<T>((X)b / s);
            }

            public static XBound<T> operator %(XBound<T> b, string s)
            {
                return new XBound<T>((X)b % s);
            }

            public static XBound<T> operator *(XBound<T> b, string s)
            {
                var x = (X)b * s;
                return new XBound<T>(x);
            }

            public static XBound<T> operator *(XBound<T> b, Func<T, string> selector)
            {
                return b * new XBound<T>(selector);
            }

            public static XBound<T> operator *(XBound<T> b, Func<T, object> selector)
            {
                return b * new XBound<T>(t => SelectorConverter(t, selector));
            }

            public static XBound<T> operator *(XBound<T> b, Func<T,int,object> expr)
            {
                return b * new XBound<T>(expr);
            }

            public static XBound<T> operator *(X x, XBound<T> placeholder)
            {
                var b = new XBound<T>(x, placeholder._data);
                b._selector = placeholder._selector;
                return b;
            }

            public static XBound<T> operator *(XBound<T> b, XBound<T> placeholder)
            {
                var nb = new XBound<T>(b, placeholder._data);
                nb._selector = placeholder._selector;
                nb._data = b._data;
                return nb;
            }
            #endregion

            #region DataBinding

            private static string SelectorConverter(T t, Func<T, object> selector)
            {
                var res = selector(t);
                return res == null ? String.Empty : res.ToString();
            }

            private IEnumerable<T> FetchData(out XBound<T> owner)
            {
                XBound<T> p = this;
                while (p != null && p._data == null)
                {
                    p = p.Parent as XBound<T>;
                }
                return (owner = (p ?? this))._data;
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

                var list = Spawn(data);

                return list;
            }
            /// <summary>
            /// Replicates current object and applies _selector to each replica
            /// </summary>
            /// <param name="data"></param>
            /// <returns></returns>
            private IEnumerable<X> Spawn(IEnumerable<T> data)
            {
                var items = new List<X>(data.Count());
                int count = 0;
                foreach (T d in data)
                {
                    var r = MakeX(d, count);
                    items.Add(r);
                    count++;
                }

                return items;
            }

            private X MakeX(T d, int count)
            {
                var x = new X(Name);
                x.Type = Type;
                if (x.StorageIsValue)
                    x.Value = _selector != null ? _selector(d, count).ToString() : Value;
                else
                {
                    var it = _selector != null ? _selector(d, count).ToString() : Value;
                    if (it != null) x.Add(X.InnerText(it));
                }

                //Attributes
                foreach (var a in Attributes)
                {
                    var ab = a as XBound<T>;
                    X ar = ab != null ? ab.MakeX(d, count) : X.Attribute(a.Name, a.Value);

                    x.AddAttribute(ar);
                }

                //Children
                foreach (var c in Children)
                {
                    var cb = c as XBound<T>;
                    X cr = null;
                    if (cb != null) cr = cb.MakeX(d, count);

                    else
                        switch (c.Type)
                        {
                            case XType.InnerText:
                                //cr = new XBound<T>(X.InnerText(c.Value)).MakeX(d);
                                cr = X.InnerText(c.Value);
                                break;
                            case XType.Element:
                                cr = new XBound<T>(X.Element(c.Name)).MakeX(d, count); //never occours?
                                break;

                        }
                    x.Add(cr);
                }
                return x;
            }

            #endregion

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
    }
}