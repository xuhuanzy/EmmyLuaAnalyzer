﻿using LuaLanguageServer.CodeAnalysis.Compilation.Infer;
using LuaLanguageServer.CodeAnalysis.Compilation.Type;
using LuaLanguageServer.CodeAnalysis.Syntax.Location;
using LuaLanguageServer.CodeAnalysis.Syntax.Node;

namespace LuaLanguageServer.CodeAnalysis.Compilation.Symbol;

public class LocalSymbol : LuaSymbol
{
    public string Name { get; }

    public LuaSyntaxElement Element { get; }

    public LuaSyntaxElement? TypeElement { get; }

    public int RetId { get; }

    public LocalSymbol(LuaSyntaxElement element, string name, LuaSyntaxElement? typeElement, int retId = 0)
        : base(SymbolKind.LocalSymbol, null)
    {
        Name = name;
        Element = element;
        TypeElement = typeElement;
        RetId = retId;
    }

    public override ILuaType GetType(SearchContext context)
    {
        var ty = context.Infer(TypeElement);
        if (ty is LuaMultiRetType multiRetType)
        {
            ty = multiRetType.GetRetType(RetId) ?? context.Compilation.Builtin.Unknown;
        }

        return ty;
    }

    public override IEnumerable<LuaLocation> GetLocations(SearchContext context)
    {
        yield return Element.Location;
    }
}