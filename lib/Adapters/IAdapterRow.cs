﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TI.Declarator.ParserCommon;

namespace Smart.Parser.Adapters
{
    public class Cell
    {
        public virtual bool IsMerged { set; get; } = false;
        public virtual int FirstMergedRow { set; get; } = -1;
        public virtual int MergedRowsCount { set; get; } = -1;
        public virtual int MergedColsCount { set; get; } = 1;
        public virtual bool IsEmpty { set; get; } = true;
        public virtual string Text { set; get; } = "";

        public virtual string GetText(bool trim = true)
        {
            var text = Text;
            if (trim)
            {
                char[] spaces = { ' ', '\n', '\r', '\t' };
                text = text.CoalesceWhitespace().Trim(spaces);
            }

            return text;
        }
        public virtual string GetTextOneLine()
        {
            return Text.Replace("\n", " ").Trim();
        }

        public int Row { get; set; } = -1;
        public int Col { get; set; } = -1;

    };

    public class Row
    {
        public Row(IAdapter adapter, int row)
        {
            this.row = row;
            this.adapter = adapter;
            Cells = adapter.GetCells(row);
        }

        public bool IsEmpty(params DeclarationField[] fields)
        {
            return fields.All(field => GetContents(field, false).IsNullOrWhiteSpace());
        }

        public int GetRowIndex()
        {
            return Cells[0].Row;
        }

        public void Merge(Row other)
        {
            for (int i = 0; i < Cells.Count(); i++)
            {
                Cells[i].Text += " " + other.Cells[i].Text;
            }
        }

        public string GetContents(DeclarationField field, bool except = true)
        {
            if (!adapter.HasDeclarationField(field))
            {
                if (!except)
                    return null;
            }
            var c = adapter.GetDeclarationField(row, field);
            if (c == null)
            {
                return "";
            }
            return c.GetText(true);
        }
        public ColumnOrdering ColumnOrdering
        {
            get
            {
                return adapter.ColumnOrdering;
            }
        }

        public bool IsEmpty()
        {
            return Cells.All(cell => cell.Text.IsNullOrWhiteSpace());
        }

        public List<Cell> Cells { get; set; }
        IAdapter adapter;
        int row;
    }

    public class Rows
    {
        private IAdapter adapter;

        // ctor etc.

        public Row this[int index]
        {
            get
            {
                return adapter.GetRow(index);
            }
        }

        public Rows(IAdapter adapter)
        {
            this.adapter = adapter;
        }
    }

}