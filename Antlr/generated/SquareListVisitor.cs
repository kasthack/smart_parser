//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.8
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from C:\tmp\smart_parser\smart_parser\Antlr\src\SquareList.g4 by ANTLR 4.8

// Unreachable code detected
#pragma warning disable 0162
// The variable '...' is assigned but its value is never used
#pragma warning disable 0219
// Missing XML comment for publicly visible type or member '...'
#pragma warning disable 1591
// Ambiguous reference in cref attribute
#pragma warning disable 419

using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using IToken = Antlr4.Runtime.IToken;

/// <summary>
/// This interface defines a complete generic visitor for a parse tree produced
/// by <see cref="SquareList"/>.
/// </summary>
/// <typeparam name="Result">The return type of the visit operation.</typeparam>
[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.8")]
[System.CLSCompliant(false)]
public interface ISquareListVisitor<Result> : IParseTreeVisitor<Result> {
	/// <summary>
	/// Visit a parse tree produced by <see cref="SquareList.bareSquares"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitBareSquares([NotNull] SquareList.BareSquaresContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="SquareList.bareScore"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitBareScore([NotNull] SquareList.BareScoreContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="SquareList.realty_id"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitRealty_id([NotNull] SquareList.Realty_idContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="SquareList.square_value_without_spaces"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSquare_value_without_spaces([NotNull] SquareList.Square_value_without_spacesContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="SquareList.square_value_with_spaces"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSquare_value_with_spaces([NotNull] SquareList.Square_value_with_spacesContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="SquareList.square_value"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSquare_value([NotNull] SquareList.Square_valueContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="SquareList.own_type"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitOwn_type([NotNull] SquareList.Own_typeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="SquareList.realty_share"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitRealty_share([NotNull] SquareList.Realty_shareContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="SquareList.square"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSquare([NotNull] SquareList.SquareContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="SquareList.realty_type"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitRealty_type([NotNull] SquareList.Realty_typeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="SquareList.country"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCountry([NotNull] SquareList.CountryContext context);
}