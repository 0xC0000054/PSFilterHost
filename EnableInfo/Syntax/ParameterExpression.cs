﻿/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2019 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System.Diagnostics;

namespace PSFilterHostDll.EnableInfo
{
    [DebuggerDisplay("{Name, nq}")]
    internal sealed class ParameterExpression : Expression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterExpression"/> class.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        public ParameterExpression(string name)
        {
            Name = name ?? string.Empty;
        }

        public string Name { get; }

        public override ExpressionType NodeType => ExpressionType.Parameter;

        public override Expression Accept(IExpressionVisitor visitor)
        {
            return visitor.VisitParameter(this);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
