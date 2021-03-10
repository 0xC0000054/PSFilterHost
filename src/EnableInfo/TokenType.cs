/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2021 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

namespace PSFilterHostDll.EnableInfo
{
    internal enum TokenType
    {
        EndOfFile,
        LeftParentheses,
        RightParentheses,
        ArgumentSeparator,
        FunctionCall,
        Identifier,
        BooleanConstant,
        IntegerConstant,
        StringConstant,
        // Logical operators
        ConditionalOr,
        ConditionalAnd,
        // Equality operators
        Equal,
        NotEqual,
        // Relational operators
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        // Multiplicative operators
        Multiply,
        Divide,
        // Additive and unary operators
        Plus,
        Minus,
        Not
    }
}
