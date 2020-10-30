using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace TI.Declarator.ParserCommon
{
    public class TColumnInfo
    {
        public DeclarationField Field;
        public int BeginColumn;
        public int EndColumn; // initialized in ColumnOrdering::FinishOrderingBuilding
        public int ColumnPixelStart; // initialized in ColumnOrdering::FinishOrderingBuilding
        public int ColumnPixelWidth;

        public override string ToString() => $"[{this.BeginColumn},{this.EndColumn})";
    }

    public class ColumnOrdering
    {
        public Dictionary<DeclarationField, TColumnInfo> ColumnOrder = new Dictionary<DeclarationField, TColumnInfo>();
        public List<TColumnInfo> MergedColumnOrder = new List<TColumnInfo>();
        public bool ManyTablesInDocument = false;
        public int? YearFromIncome = null;
        public static bool SearchForFioColumnOnly = false;

        public bool ContainsField(DeclarationField field) => this.ColumnOrder.ContainsKey(field);

        public void Add(TColumnInfo s)
        {
            if (this.ColumnOrder.ContainsKey(s.Field))
            {
                return;
            }

            this.ColumnOrder.Add(s.Field, s);
        }

        public void Delete(DeclarationField field) => this.ColumnOrder.Remove(field);

        public void FinishOrderingBuilding(int tableIndention)
        {
            this.MergedColumnOrder.Clear();
            foreach (var x in this.ColumnOrder.Values)
            {
                this.MergedColumnOrder.Add(x);
            }

            this.MergedColumnOrder.Sort((x, y) => x.BeginColumn.CompareTo(y.BeginColumn));
            var sumwidth = tableIndention;
            foreach (var x in this.MergedColumnOrder)
            {
                x.ColumnPixelStart = sumwidth;
                sumwidth += x.ColumnPixelWidth;
            }
        }

        public static int PeriodIntersection(int start1, int end1, int start2, int end2) => start1 <= end2 && start2 <= end1 ? Math.Min(end1, end2) - Math.Max(start1, start2) : 0;

        public DeclarationField FindByPixelIntersection(int start, int end, out int maxInterSize)
        {
            var field = DeclarationField.None;
            maxInterSize = 0;
            foreach (var x in this.ColumnOrder)
            {
                var interSize = PeriodIntersection(start, end, x.Value.ColumnPixelStart, x.Value.ColumnPixelStart + x.Value.ColumnPixelWidth);
                if (interSize > maxInterSize)
                {
                    maxInterSize = interSize;
                    field = x.Key;
                }
            }

            return field;
        }

        public int GetMaxColumnEndIndex()
        {
            Debug.Assert(this.MergedColumnOrder.Count > 0);
            return this.MergedColumnOrder[this.MergedColumnOrder.Count - 1].EndColumn;
        }

        public bool OwnershipTypeInSeparateField => this.ColumnOrder.ContainsKey(DeclarationField.OwnedRealEstateOwnershipType);

        public int FirstDataRow { get; set; } = 1;

        public string Title { get; set; }

        public string MinistryName { get; set; }

        public string Section { get; set; }

        public int? Year { get; set; }

        public int? HeaderBegin { get; set; }

        public int? HeaderEnd { get; set; }

        public int GetPossibleHeaderBegin() => this.HeaderBegin ?? 0;

        public int GetPossibleHeaderEnd() => this.HeaderEnd ?? this.GetPossibleHeaderBegin() + 2;
    }
}
