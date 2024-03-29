﻿/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

namespace PSFilterHostDll.EnableInfo
{
    internal interface IExpressionVisitor
    {
        Expression VisitBinary(BinaryExpression node);
        Expression VisitConstant(ConstantExpression node);
        Expression VisitFunctionCall(FunctionCallExpression node);
        Expression VisitLogical(LogicalExpression node);
        Expression VisitParameter(ParameterExpression node);
        Expression VisitUnary(UnaryExpression node);
    }
}
