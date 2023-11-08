﻿using LuaLanguageServer.CodeAnalysis.Compile.Parser;
using LuaLanguageServer.CodeAnalysis.Kind;

namespace LuaLanguageServer.CodeAnalysis.Compile.Grammar.Doc;

public static class TypesParser
{
    public static void TypeList(LuaDocParser p)
    {
        var cm = Type(p);
        while (cm.IsComplete && p.Current is LuaTokenKind.TkComma)
        {
            p.Bump();
            cm = Type(p);
        }
    }

    public static CompleteMarker Type(LuaDocParser p, bool unionType = true)
    {
        var cm = PrimaryType(p);
        if (!cm.IsComplete)
        {
            return cm;
        }

        // suffix
        SuffixType(p, ref cm);
        // ReSharper disable once InvertIf
        if (unionType && p.Current is LuaTokenKind.TkDocOr)
        {
            var m = cm.Precede(p);
            p.Bump();

            var cm2 = Type(p, false);
            while (cm2.IsComplete && p.Current is LuaTokenKind.TkDocOr)
            {
                p.Bump();
                cm2 = Type(p, false);
            }

            cm = m.Complete(p, LuaSyntaxKind.TypeUnion);
        }

        return cm;
    }

    public static CompleteMarker AliasType(LuaDocParser p)
    {
        if (p.Current is not LuaTokenKind.TkDocOr)
        {
            return Type(p);
        }
        var m = p.Marker();
        p.Bump();

        var cm2 = Type(p, false);
        while (cm2.IsComplete && p.Current is LuaTokenKind.TkDocOr)
        {
            p.Bump();
            cm2 = Type(p, false);
        }

        return m.Complete(p, LuaSyntaxKind.TypeUnion);
    }

    private static void SuffixType(LuaDocParser p, ref CompleteMarker pcm)
    {
        bool continueArray = false;
        while (true)
        {
            switch (p.Current)
            {
                // array
                case LuaTokenKind.TkLeftBracket:
                {
                    var m = pcm.Precede(p);
                    p.Bump();
                    p.Expect(LuaTokenKind.TkRightBracket);
                    pcm = m.Complete(p, LuaSyntaxKind.TypeArray);
                    continueArray = true;
                    break;
                }
                // generic
                case LuaTokenKind.TkLt:
                {
                    if (continueArray)
                    {
                        return;
                    }

                    var m = pcm.Precede(p);
                    p.Bump();
                    TypeList(p);
                    p.Expect(LuaTokenKind.TkGt);
                    pcm = m.Complete(p, LuaSyntaxKind.TypeGeneric);
                    return;
                }
                default:
                {
                    return;
                }
            }
        }
    }

    private static CompleteMarker PrimaryType(LuaDocParser p)
    {
        return p.Current switch
        {
            LuaTokenKind.TkLeftBrace => TableType(p),
            LuaTokenKind.TkLeftParen => ParenType(p),
            LuaTokenKind.TkLeftBracket => TupleType(p),
            LuaTokenKind.TkString or LuaTokenKind.TkInt => LiteralType(p),
            LuaTokenKind.TkName => OtherType(p),
            _ => CompleteMarker.Empty
        };
    }

    public static CompleteMarker TableType(LuaDocParser p)
    {
        var m = p.Marker();
        p.Bump();

        try
        {
            if (p.Current is LuaTokenKind.TkRightBrace)
            {
                p.Bump();
                return m.Complete(p, LuaSyntaxKind.TypeTable);
            }

            var cm = TypedField(p);
            while (cm.IsComplete && (p.Current is LuaTokenKind.TkComma or LuaTokenKind.TkSemicolon))
            {
                p.Bump();
                cm = TypedField(p);
            }

            p.Expect(LuaTokenKind.TkRightBrace);

            return m.Complete(p, LuaSyntaxKind.TypeTable);
        }
        catch (UnexpectedTokenException e)
        {
            return m.Fail(p, LuaSyntaxKind.TypeTable, e.Message);
        }
    }

    public static CompleteMarker TypedField(LuaDocParser p)
    {
        var m = p.Marker();
        try
        {
            switch (p.Current)
            {
                case LuaTokenKind.TkName:
                {
                    p.Bump();
                    break;
                }
                case LuaTokenKind.TkLeftBracket:
                {
                    p.Bump();
                    switch (p.Current)
                    {
                        case LuaTokenKind.TkString or LuaTokenKind.TkInt:
                        {
                            p.Bump();
                            break;
                        }
                        default:
                        {
                            Type(p);
                            break;
                        }
                    }

                    p.Expect(LuaTokenKind.TkRightBracket);
                    break;
                }
            }

            p.Expect(LuaTokenKind.TkColon);
            Type(p);
            return m.Complete(p, LuaSyntaxKind.TypedField);
        }
        catch (UnexpectedTokenException e)
        {
            return m.Fail(p, LuaSyntaxKind.TypedField, e.Message);
        }
    }

    private static CompleteMarker ParenType(LuaDocParser p)
    {
        var m = p.Marker();
        p.Bump();

        try
        {
            Type(p);

            p.Expect(LuaTokenKind.TkRightParen);

            return m.Complete(p, LuaSyntaxKind.TypeParen);
        }
        catch (UnexpectedTokenException e)
        {
            return m.Fail(p, LuaSyntaxKind.TypeParen, e.Message);
        }
    }

    private static CompleteMarker TupleType(LuaDocParser p)
    {
        var m = p.Marker();
        p.Bump();

        try
        {
            if (p.Current is LuaTokenKind.TkRightBracket)
            {
                p.Bump();
                return m.Complete(p, LuaSyntaxKind.TypeTuple);
            }

            TypeList(p);

            p.Expect(LuaTokenKind.TkRightBracket);

            return m.Complete(p, LuaSyntaxKind.TypeTuple);
        }
        catch (UnexpectedTokenException e)
        {
            return m.Fail(p, LuaSyntaxKind.TypeTuple, e.Message);
        }
    }

    private static CompleteMarker LiteralType(LuaDocParser p)
    {
        var m = p.Marker();

        p.Bump();

        return m.Complete(p, LuaSyntaxKind.TypeLiteral);
    }

    private static CompleteMarker OtherType(LuaDocParser p)
    {
        if (p.CurrentNameText is "fun")
        {
            return FunType(p);
        }

        var m = p.Marker();
        p.Bump();
        return m.Complete(p, LuaSyntaxKind.TypeName);
    }

    public static CompleteMarker FunType(LuaDocParser p)
    {
        var m = p.Marker();
        try
        {
            p.Expect(LuaTokenKind.TkName);
            p.Expect(LuaTokenKind.TkLeftParen);
            if (p.Current != LuaTokenKind.TkRightParen)
            {
                var cm = TypedParameter(p);
                while (cm.IsComplete && p.Current is LuaTokenKind.TkComma)
                {
                    p.Bump();
                    cm = TypedParameter(p);
                }

                if (p.Current is LuaTokenKind.TkDots)
                {
                    p.Bump();
                    if (p.Current is LuaTokenKind.TkColon)
                    {
                        p.Bump();
                        Type(p);
                    }
                }
            }

            p.Expect(LuaTokenKind.TkRightParen);
            // ReSharper disable once InvertIf
            if (p.Current is LuaTokenKind.TkColon)
            {
                p.Bump();
                Type(p);
            }

            return m.Complete(p, LuaSyntaxKind.TypeFun);
        }
        catch (UnexpectedTokenException e)
        {
            return m.Fail(p, LuaSyntaxKind.TypeFun, e.Message);
        }
    }

    public static CompleteMarker TypedParameter(LuaDocParser p)
    {
        var m = p.Marker();
        try
        {
            if (p.Current is LuaTokenKind.TkName)
            {
                p.Bump();
            }
            else if (p.Current is LuaTokenKind.TkDots)
            {
                return CompleteMarker.Empty;
            }
            else
            {
                return m.Fail(p, LuaSyntaxKind.TypedParameter, "expect <name> or '...'");
            }

            if (p.Current is LuaTokenKind.TkColon)
            {
                p.Bump();
                Type(p);
            }

            return m.Complete(p, LuaSyntaxKind.TypedParameter);
        }
        catch (UnexpectedTokenException)
        {
            return m.Fail(p, LuaSyntaxKind.TypedParameter, "expect typed parameter");
        }
    }
}
