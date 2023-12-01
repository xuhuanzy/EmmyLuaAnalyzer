﻿using LuaLanguageServer.CodeAnalysis.Compilation.Infer;
using LuaLanguageServer.CodeAnalysis.Compilation.Type;
using LuaLanguageServer.CodeAnalysis.Syntax.Location;
using LuaLanguageServer.CodeAnalysis.Syntax.Node;

namespace LuaLanguageServer.CodeAnalysis.Compilation.Symbol;

public class FieldSymbol : LuaSymbol
{
    public string Name { get; }

    public LuaSyntaxElement? TypeElement { get; }

    public LuaSyntaxElement Element { get; }

    public FieldSymbol(LuaSyntaxElement element, string name, LuaSyntaxElement? typeElement, ILuaType containingType)
        : base(SymbolKind.FieldSymbol, containingType)
    {
        Element = element;
        Name = name;
        TypeElement = typeElement;
    }

    public override ILuaType GetType(SearchContext context)
    {
        return context.Infer(TypeElement);
    }

    public override IEnumerable<LuaLocation> GetLocations(SearchContext context)
    {
        yield return Element.Location;
    }
}