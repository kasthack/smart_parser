//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.8
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from C:\tmp\smart_parser\smart_parser\Antlr\src\SoupParser.g4 by ANTLR 4.8

// Unreachable code detected
#pragma warning disable 0162
// The variable '...' is assigned but its value is never used
#pragma warning disable 0219
// Missing XML comment for publicly visible type or member '...'
#pragma warning disable 1591
// Ambiguous reference in cref attribute
#pragma warning disable 419

using Antlr4.Runtime.Misc;
using IParseTreeListener = Antlr4.Runtime.Tree.IParseTreeListener;
using IToken = Antlr4.Runtime.IToken;

/// <summary>
/// This interface defines a complete listener for a parse tree produced by
/// <see cref="SoupParser"/>.
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.8")]
[System.CLSCompliant(false)]
public interface ISoupParserListener : IParseTreeListener {
	/// <summary>
	/// Enter a parse tree produced by <see cref="SoupParser.any_realty_item_list"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterAny_realty_item_list([NotNull] SoupParser.Any_realty_item_listContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="SoupParser.any_realty_item_list"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitAny_realty_item_list([NotNull] SoupParser.Any_realty_item_listContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="SoupParser.any_realty_item"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterAny_realty_item([NotNull] SoupParser.Any_realty_itemContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="SoupParser.any_realty_item"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitAny_realty_item([NotNull] SoupParser.Any_realty_itemContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="SoupParser.realty_id"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterRealty_id([NotNull] SoupParser.Realty_idContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="SoupParser.realty_id"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitRealty_id([NotNull] SoupParser.Realty_idContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="SoupParser.square_value_without_spaces"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterSquare_value_without_spaces([NotNull] SoupParser.Square_value_without_spacesContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="SoupParser.square_value_without_spaces"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitSquare_value_without_spaces([NotNull] SoupParser.Square_value_without_spacesContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="SoupParser.square_value_with_spaces"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterSquare_value_with_spaces([NotNull] SoupParser.Square_value_with_spacesContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="SoupParser.square_value_with_spaces"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitSquare_value_with_spaces([NotNull] SoupParser.Square_value_with_spacesContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="SoupParser.square_value"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterSquare_value([NotNull] SoupParser.Square_valueContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="SoupParser.square_value"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitSquare_value([NotNull] SoupParser.Square_valueContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="SoupParser.own_type"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterOwn_type([NotNull] SoupParser.Own_typeContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="SoupParser.own_type"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitOwn_type([NotNull] SoupParser.Own_typeContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="SoupParser.realty_share"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterRealty_share([NotNull] SoupParser.Realty_shareContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="SoupParser.realty_share"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitRealty_share([NotNull] SoupParser.Realty_shareContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="SoupParser.square"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterSquare([NotNull] SoupParser.SquareContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="SoupParser.square"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitSquare([NotNull] SoupParser.SquareContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="SoupParser.realty_type"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterRealty_type([NotNull] SoupParser.Realty_typeContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="SoupParser.realty_type"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitRealty_type([NotNull] SoupParser.Realty_typeContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="SoupParser.country"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterCountry([NotNull] SoupParser.CountryContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="SoupParser.country"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitCountry([NotNull] SoupParser.CountryContext context);
}
