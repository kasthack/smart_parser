using System;
using System.Collections;
using System.Collections.Generic;

namespace Smart.Parser.Adapters
{
    internal class AsposeDocCell : Cell
    {
        public AsposeDocCell(Aspose.Words.Tables.Cell cell)
        {
            if (cell == null)
            {
                return;
            }

            var cellText = cell.ToString(Aspose.Words.SaveFormat.Text).Trim();

            this.Text = cellText;

            this.IsEmpty = string.IsNullOrEmpty(this.Text);
        }
    }

    public class AsposeDocAdapter : IAdapter
    {
        public static IAdapter CreateAdapter(string fileName) => new AsposeDocAdapter(fileName);

        public override Cell GetCell(int row, int column)
        {
            var cell = this.table.Rows[row].Cells[column];
            return new AsposeDocCell(cell);
        }

        public override List<Cell> GetCells(int row, int maxColEnd = -1)
        {
            var index = 0;
            var result = new List<Cell>();
            IEnumerator enumerator = this.table.Rows[row].GetEnumerator();
            const int range_end = -1;
            while (enumerator.MoveNext())
            {
                var cell = (Aspose.Words.Tables.Cell)enumerator.Current;
                if (index < range_end)
                {
                    index++;
                    continue;
                }

                result.Add(new AsposeDocCell(cell));

                index++;
            }
            return result;
        }

        public override int GetRowsCount() => this.table.Rows.Count;

        public override int GetColsCount() => this.table.Rows[0].Count;

        public override string GetTitleOutsideTheTable() => this.title;

        private AsposeDocAdapter(string fileName)
        {
            this.DocumentFile = fileName;
            var doc = new Aspose.Words.Document(fileName);
            var tables = doc.GetChildNodes(Aspose.Words.NodeType.Table, true);

            var count = tables.Count;
            if (count == 0)
            {
                throw new SystemException("No table found in document " + fileName);
            }

            this.table = (Aspose.Words.Tables.Table)tables[0];

            Aspose.Words.Node node = this.table;
            while (node.PreviousSibling != null)
            {
                node = node.PreviousSibling;
            }
            var text = "";
            while (node.NextSibling != this.table)
            {
                text += node.ToString();
                node = node.NextSibling;
            }

            this.title = text;
        }

        private readonly Aspose.Words.Tables.Table table;
        private readonly string title;
    }
}
