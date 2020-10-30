using System.Collections.Generic;
using System.Linq;

namespace Smart.Parser.Adapters
{
    // using Smart.Parser.Row;
    public class TSectionPredicates
    {
        private static bool CheckSectionLanguageModel(string cellText)
        {
            if (cellText.Contains("Сведения о"))
            {
                return true;
            }

            // first words: get it from previous results:
            // ~/media/json$ ls | xargs  jq -cr '.persons[].person.department' | awk '{print $1}' | sort | uniq -c  | sort -nr
            // стоит перейти на более продвинутую модель на триграммах
            return cellText.StartsWith("ФК") ||
                cellText.StartsWith("ФГ") ||
                cellText.StartsWith("ГУ") ||
                cellText.StartsWith("Федеральн") ||
                cellText.StartsWith("федеральн") ||
                cellText.StartsWith("ФБУ") ||
                cellText.StartsWith("Руководство") ||
                cellText.StartsWith("ФАУ") ||
                cellText.StartsWith("Департамент") ||
                cellText.StartsWith("Заместители") ||
                cellText.StartsWith("Институт") ||
                cellText.StartsWith("Государственное") ||
                cellText.StartsWith("Главное") ||
                cellText.StartsWith("Отдел") ||
                cellText.StartsWith("Управлени") ||
                cellText.StartsWith("Фонд") ||
                cellText.StartsWith("АНО") ||
                cellText.StartsWith("УФСИН") ||
                cellText.StartsWith("Центр") ||
                cellText.StartsWith("ФСИН") ||
                cellText.StartsWith("Министерств") ||
                cellText.StartsWith("Лица") ||
                cellText.StartsWith("ИК");
        }

        public static bool IsSectionRow(List<Cell> cells, int colsCount, bool prevRowIsSection, out string text)
        {
            text = null;
            if (cells.Count == 0)
            {
                return false;
            }

            var maxMergedCols = 0;
            var maxCellWidth = 0;
            var rowText = string.Empty;
            var cellText = string.Empty;
            var cellsWithTextCount = 0;
            var allWidth = 0;
            foreach (var c in cells)
            {
                var trimmedText = c.Text.Trim(' ', '\n');
                if (c.MergedColsCount > maxMergedCols)
                {
                    cellText = trimmedText;
                    maxMergedCols = c.MergedColsCount;
                }

                if (trimmedText.Length > 0)
                {
                    rowText += c.Text;
                    cellsWithTextCount++;
                    if (c.CellWidth > maxCellWidth)
                    {
                        maxCellWidth = c.CellWidth;
                    }
                }

                allWidth += c.CellWidth;
            }

            rowText = rowText.Trim(' ', '\n');
            var manyColsAreMerged = maxMergedCols > colsCount * 0.45;
            var OneColumnIsLarge = maxCellWidth > 1000 || maxCellWidth >= allWidth * 0.3;
            var langModel = CheckSectionLanguageModel(cellText);
            var hasEnoughLength = rowText.Length >= 9; // "Референты"; но встречаются ещё "Заместители Министра"
            var halfCapitalLetters = rowText.Count(char.IsUpper) * 2 > rowText.Length;

            // Stop Words
            var stopWords = new List<string> { "сведения", };
            var hasStopWord = false;
            foreach (var word in stopWords)
            {
                if (rowText.ToLower() == word)
                {
                    hasStopWord = true;
                }
            }

            if (hasStopWord)
            {
                return false;
            }

            // "ННИИПК", "СамГМУ"
            if (!hasEnoughLength && !halfCapitalLetters)
            {
                return false;
            }

            if (!OneColumnIsLarge)
            {
                return false;
            }

            if (cellsWithTextCount == 1)
            {
                // possible title, exact number of not empty columns is not yet defined
                if (maxMergedCols > 5 && langModel)
                {
                    text = rowText;
                    return true;
                }

                if (manyColsAreMerged)
                {
                    text = rowText;
                    return true;
                }
            }

            if (cellsWithTextCount <= 2 && manyColsAreMerged && langModel)
            {
                text = cellText;
                return true;
            }

            // в начале могут быть многострочные заголовки, которые обычно начинаются с маленькой буквы
            if (prevRowIsSection && hasEnoughLength && cells[0].Row < 10 && char.IsLower(rowText[0]))
            {
                text = rowText;
                return true;
            }

            return false;
        }
    }
}
