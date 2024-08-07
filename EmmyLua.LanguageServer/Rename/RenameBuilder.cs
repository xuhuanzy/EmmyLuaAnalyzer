﻿using EmmyLua.CodeAnalysis.Compilation.Search;
using EmmyLua.CodeAnalysis.Compilation.Semantic;
using EmmyLua.CodeAnalysis.Compile.Lexer;
using EmmyLua.CodeAnalysis.Document;
using EmmyLua.CodeAnalysis.Syntax.Node;
using EmmyLua.CodeAnalysis.Syntax.Node.SyntaxNodes;
using EmmyLua.LanguageServer.Framework.Protocol.Model;
using EmmyLua.LanguageServer.Framework.Protocol.Model.TextEdit;
using EmmyLua.LanguageServer.Server;
using EmmyLua.LanguageServer.Util;

namespace EmmyLua.LanguageServer.Rename;

public class RenameBuilder(ServerContext context)
{
    public Dictionary<DocumentUri, List<TextEdit>> Build(SemanticModel semanticModel, LuaSyntaxElement element,
        string newName)
    {
        if (newName.Length == 0)
        {
            return new();
        }

        newName = newName.Trim();
        var references = semanticModel.FindReferences(element);
        var notSymbolChar = !LuaLexer.IsNameStart(newName.First());
        if (!notSymbolChar)
        {
            notSymbolChar = newName.Skip(1).Any(it => !LuaLexer.IsNameContinue(it));
        }

        var changes = new Dictionary<DocumentUri, List<TextEdit>>();
        foreach (var reference in references)
        {
            switch (reference.Element)
            {
                case LuaStringToken stringToken:
                {
                    ChangeStringToken(changes, stringToken, reference, newName);
                    break;
                }
                case LuaNameToken nameToken:
                {
                    ChangeNameToken(changes, nameToken, reference, newName, notSymbolChar);
                    break;
                }
                case LuaLocalNameSyntax localNameSyntax:
                {
                    ChangeLocalName(changes, localNameSyntax, reference, newName, notSymbolChar);
                    break;
                }
                case LuaNameExprSyntax nameExprSyntax:
                {
                    if (nameExprSyntax.Name is not null)
                    {
                        ChangeNameToken(changes, nameExprSyntax.Name, reference, newName, notSymbolChar);
                    }

                    break;
                }
                case LuaParamDefSyntax paramDefSyntax:
                {
                    if (paramDefSyntax.Name is not null)
                    {
                        ChangeNameToken(changes, paramDefSyntax.Name, reference, newName, notSymbolChar);
                    }
                    break;
                }
            }
        }

        return changes;
    }

    private void AddChange(Dictionary<DocumentUri, List<TextEdit>> changes, LuaLocation location, string newName)
    {
        var uri = location.Uri;
        if (!changes.TryGetValue(uri, out var edits))
        {
            edits = new List<TextEdit>();
            changes[uri] = edits;
        }

        var edit = new TextEdit()
        {
            Range = location.ToLspRange(),
            NewText = newName
        };
        edits.Add(edit);
    }

    private void ChangeStringToken(Dictionary<DocumentUri, List<TextEdit>> changes, LuaStringToken stringToken,
        ReferenceResult referenceResult, string newName)
    {
        var range = stringToken.Range;
        if (range.Length < 2)
        {
            return;
        }

        var document = context.LuaProject.GetDocumentByUri(referenceResult.Location.Uri);
        if (document is not null)
        {
            range = new SourceRange(StartOffset: range.StartOffset + 1, Length: range.Length - 2);
            AddChange(changes, document.GetLocation(range), newName);
        }
    }

    private void ChangeNameToken(Dictionary<DocumentUri, List<TextEdit>> changes, LuaNameToken nameToken,
        ReferenceResult referenceResult, string newName, bool notSymbolChar)
    {
        // give up rename
        if (notSymbolChar)
        {
            return;
        }

        AddChange(changes, referenceResult.Location, newName);
    }
    
    private void ChangeLocalName(Dictionary<DocumentUri, List<TextEdit>> changes, LuaLocalNameSyntax localNameSyntax,
        ReferenceResult referenceResult, string newName, bool notSymbolChar)
    {
        // give up rename
        if (notSymbolChar)
        {
            return;
        }
        
        AddChange(changes, referenceResult.Location, newName);
    }
}