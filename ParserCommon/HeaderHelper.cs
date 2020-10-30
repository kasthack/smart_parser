using System;
using System.Text.RegularExpressions;

namespace TI.Declarator.ParserCommon
{
    public static class HeaderHelpers
    {
        public static bool HasRealEstateStr(string str)
        {
            var strLower = str.ToLower().Replace("-", "");
            return strLower.Contains("недвижимости")
                || strLower.Contains("недвижимого")
                || strLower.Replace(" ", "").Contains("иноенедвижимоеимущество(кв.м)", StringComparison.OrdinalIgnoreCase);
        }
        public static DeclarationField GetField(string str)
        {
            var field = TryGetField(str);
            return field == DeclarationField.None ? throw new Exception($"Could not determine column type for header {str}.") : field;
        }

        public static DeclarationField TryGetField(string str)
        {
            str = NormalizeString(str);
            if (str.IsNumber()) { return DeclarationField.Number; }
            if (str.IsNameAndOccupation()) { return DeclarationField.NameAndOccupationOrRelativeType; }
            if (str.IsName()) { return DeclarationField.NameOrRelativeType; }
            if (str.IsRelativeType()) { return DeclarationField.RelativeTypeStrict; }
            if (str.IsOccupation()) { return DeclarationField.Occupation; }
            if (str.IsDepartment() && !str.IsDeclaredYearlyIncome()) { return DeclarationField.Department; }

            if (str.IsSpendingsField()) { return DeclarationField.Spendings; }

            if (str.IsMixedRealEstateType()) { return DeclarationField.MixedRealEstateType; }
            if (str.IsMixedRealEstateSquare() && !str.IsMixedRealEstateCountry()) { return DeclarationField.MixedRealEstateSquare; }
            if (str.IsMixedRealEstateCountry() && !str.IsMixedRealEstateSquare() ) { return DeclarationField.MixedRealEstateCountry; }
            if (str.IsMixedRealEstateOwnershipType() && !str.IsMixedRealEstateSquare()) { return DeclarationField.MixedRealEstateOwnershipType; }
            if (str.IsMixedLandAreaSquare()) { return DeclarationField.MixedLandAreaSquare; }
            if (str.IsMixedLivingHouseSquare()) { return DeclarationField.MixedLivingHouseSquare; }
            if (str.IsMixedAppartmentSquare()) { return DeclarationField.MixedAppartmentSquare; }
            if (str.IsMixedSummerHouseSquare()) { return DeclarationField.MixedSummerHouseSquare; }
            if (str.IsMixedGarageSquare()) { return DeclarationField.MixedGarageSquare; }

            if (str.IsOwnedRealEstateType()) { return DeclarationField.OwnedRealEstateType; }
            if (str.IsOwnedRealEstateOwnershipType()) { return DeclarationField.OwnedRealEstateOwnershipType; }
            if (str.IsOwnedRealEstateSquare()) { return DeclarationField.OwnedRealEstateSquare; }
            if (str.IsOwnedRealEstateCountry()) { return DeclarationField.OwnedRealEstateCountry; }

            if (str.IsStatePropertyType()) { return DeclarationField.StatePropertyType; }
            if (str.IsStatePropertySquare()) { return DeclarationField.StatePropertySquare; }
            if (str.IsStatePropertyCountry()) { return DeclarationField.StatePropertyCountry; }
            if (str.IsStatePropertyOwnershipType()) { return DeclarationField.StatePropertyOwnershipType; }

            if (str.HasChild() && str.IsVehicle() && !(str.HasMainDeclarant() || str.HasSpouse())) {
                return DeclarationField.ChildVehicle; }

            if (str.HasSpouse() && str.IsVehicle() && !(str.HasMainDeclarant() || str.HasChild()))  {
                return DeclarationField.SpouseVehicle; }
            if (str.HasMainDeclarant() && str.IsVehicle()) { return DeclarationField.DeclarantVehicle; }

            if (str.IsVehicleType()) { return DeclarationField.VehicleType; }
            if (str.IsVehicleModel()) { return DeclarationField.VehicleModel; }
            if (str.IsVehicle()) { return DeclarationField.Vehicle; }
            if (str.IsDeclaredYearlyIncomeThousands()) {
                if (str.HasChild()) { return DeclarationField.ChildIncomeInThousands; }
                if (str.HasSpouse()) { return DeclarationField.SpouseIncomeInThousands; }
                return str.HasMainDeclarant() ? DeclarationField.DeclarantIncomeInThousands : DeclarationField.DeclaredYearlyIncomeThousands;
            }

            if (str.IsDeclaredYearlyIncome())
            {
                if (str.HasChild() && !(str.HasMainDeclarant() || str.HasSpouse())) { return DeclarationField.ChildIncome; }
                if (str.HasSpouse() && !(str.HasMainDeclarant() || str.HasChild())) { return DeclarationField.SpouseIncome; }
                return str.HasMainDeclarant() ? DeclarationField.DeclarantIncome : DeclarationField.DeclaredYearlyIncome;
            }

            if (str.IsMainWorkPositionIncome())
            {
                return DeclarationField.MainWorkPositionIncome;
            }

            if (str.IsDataSources()) { return DeclarationField.DataSources; }
            if (str.IsComments()) { return DeclarationField.Comments; }

            if (str.IsMixedRealEstateDeclarant()) { return DeclarationField.DeclarantMixedColumnWithNaturalText; }
            if (str.IsMixedRealEstateSpouse()) { return DeclarationField.SpouseMixedColumnWithNaturalText; }
            if (str.IsMixedRealEstateChild()) { return DeclarationField.ChildMixedColumnWithNaturalText; }

            if (str.IsMixedRealEstate()) { return DeclarationField.MixedColumnWithNaturalText; }
            if (str.IsOwnedRealEstate()) { return DeclarationField.OwnedColumnWithNaturalText; }
            if (str.IsStateRealEstate()) { return DeclarationField.StateColumnWithNaturalText; }
            if (HasCountryString(str) && HasRealEstateStr(str)) { return DeclarationField.MixedRealEstateCountry; }
            if (HasRealEstateStr(str)) { return DeclarationField.MixedColumnWithNaturalText; }

            if (str.IsAcquiredProperty()) { return DeclarationField.AcquiredProperty; }
            if (str.IsTransactionSubject()) { return DeclarationField.TransactionSubject; }
            if (str.IsMoneySources()) { return DeclarationField.MoneySources; }
            if (str.IsMoneyOnBankAccounts()) { return DeclarationField.MoneyOnBankAccounts; }
            if (str.IsSecuritiesField()) { return DeclarationField.Securities; }
            if (str.IsStocksField()) { return DeclarationField.Stocks; }

            if (HasSquareString(str)) { return DeclarationField.MixedRealEstateSquare; }
            return HasCountryString(str) ? DeclarationField.MixedRealEstateCountry : DeclarationField.None;
        }

        private static string NormalizeString(string str) => string.Join(" ", str.ToLower().Split(new char[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                         .RemoveStupidTranslit();

        public static bool IsNumber(this string str)
        {
            str = str.Replace(" ", "");
            return str.StartsWith("№") ||
                   str.Contains("nп/п", StringComparison.OrdinalIgnoreCase) ||
                   str.Contains("№п/п", StringComparison.OrdinalIgnoreCase) ||
                   str.ToLower().Replace("\\", "/").Equals("п/п") ||
                   str.Contains("nпп", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsName(this string s)
        {
            var clean = s.Replace(",", "").Replace("-", "").Replace("\n", "").Replace(" ", "").ToLower();
            return clean.Contains("фамилия") ||
                    clean.Contains("фамилимя") ||
                    clean.StartsWith("лицаодоходах") ||
                    clean.StartsWith("подающиесведения") ||
                    clean.StartsWith("подающийсведения") ||
                    clean.Contains("фио") ||
                    clean.Contains(".иф.о.") ||
                    clean.Contains("сведенияодепутате") ||
                    clean.Contains("ф.и.о");
        }
        public static bool IsNameAndOccupation(this string s) => (s.IsName() && s.IsOccupation())
                   || s.OnlyRussianLowercase().Contains("замещаемаядолжностьстепеньродства");

        private static bool IsRelativeType(this string s) => (s.Contains("члены семьи") || s.Contains("степень родства")) && !s.IsName();

        private static bool IsOccupation(this string s)
        {
            var clean = s.Replace("-", "").Replace(" ", "").ToLower();
            return clean.Contains("должность") ||
                    clean.Contains("должности") ||
                    clean.Contains("должностей");
        }

        private static bool IsDepartment(this string s) => s.Contains("наименование организации") || s.Contains("ерриториальное управление в субъекте");

        private static bool IsMixedRealEstateOwnershipType(this string s) => s.IsMixedColumn() && HasOwnershipTypeString(s);

        public static string OnlyRussianLowercase(this string s) => Regex.Replace(s.ToLower(), "[^а-яё]", "");

        private static bool HasRealEstateTypeStr(this string s)
        {
            var clean = s.OnlyRussianLowercase();
            return clean.Contains("видобъекта") ||
                    clean.Contains("видобъектов") ||
                    clean.Contains("видобьекта") ||
                    clean.Contains("видимущества") ||
                    clean.Contains("видыобъектов") ||
                    clean.Contains("видынедвижимости") ||
                    clean.Contains("видинаименованиеимущества") ||
                    clean.Contains("виднедвижимости");
        }

        private static bool HasOwnershipTypeString(this string s)
        {
            var clean = s.OnlyRussianLowercase();
            return Regex.Match(clean, "вид((собстве..ост)|(правана))").Success;
        }
        private static bool HasStateString(this string s)
        {
            var clean = s.OnlyRussianLowercase();
            return clean.Contains("пользовани");
        }
        private static bool HasOwnedString(this string s)
        {
            var clean = s.OnlyRussianLowercase();
            return clean.Contains("собственности") || clean.Contains("принадлежащие");
        }

        private static bool HasSquareString(this string s)
        {
            var clean = s.OnlyRussianLowercase();
            return clean.Contains("площадь");
        }

        private static bool HasCountryString(this string s)
        {
            var clean = s.OnlyRussianLowercase();
            return clean.Contains("страна") || clean.Contains("регион");
        }

        public static bool IsStateColumn(this string s) => !HasOwnedString(s)
                    && HasStateString(s);

        public static bool IsOwnedColumn(this string s) => HasOwnedString(s)
                    && !HasStateString(s);

        public static bool IsMixedColumn(this string s) => HasOwnedString(s)
                    && HasStateString(s);

        private static bool IsOwnedRealEstateType(this string s) => IsOwnedColumn(s) && HasRealEstateTypeStr(s);

        private static bool IsOwnedRealEstateOwnershipType(this string s) => IsOwnedColumn(s) && HasOwnershipTypeString(s);

        private static bool IsOwnedRealEstateSquare(this string s) => IsOwnedColumn(s) && HasSquareString(s) && !s.Contains("вид");

        private static bool IsOwnedRealEstateCountry(this string s) => IsOwnedColumn(s) && HasCountryString(s);

        private static bool IsStatePropertyType(this string s) => (IsStateColumn(s) && HasRealEstateTypeStr(s)) || s.Equals("Объекты недвижимости, находящиеся в вид объекта");
        private static bool IsStatePropertyOwnershipType(this string s) => HasStateString(s) && HasOwnershipTypeString(s);
        private static bool IsStatePropertySquare(this string s) => IsStateColumn(s) && HasSquareString(s) && !s.Contains("вид");

        private static bool IsStatePropertyCountry(this string s) => IsStateColumn(s) && HasCountryString(s);

        private static bool IsMixedRealEstateType(this string s) => IsMixedColumn(s) && HasRealEstateTypeStr(s);

        private static bool HasMainDeclarant(this string s)
        {
            s = s.OnlyRussianLowercase();
            return (
                           s.Contains("служащего")
                        || s.Contains("служащему")
                        || s.Contains("должностлицо")
                        || s.Contains("должнослицо")
                        || s.Contains("должностноелицо")
                   )
                && !HasChild(s) && !HasSpouse(s);
        }

        private static bool HasChild(this string s) => s.Contains("детей") || s.Contains("детям");
        private static bool HasSpouse(this string s) => s.Contains("супруг");
        private static bool IsMixedRealEstateDeclarant(this string s) => IsMixedColumn(s) && HasRealEstateStr(s) && HasMainDeclarant(s) && !HasSpouse(s);
        private static bool IsMixedRealEstateChild(this string s) => IsMixedColumn(s) && HasRealEstateStr(s) && HasChild(s) && !HasSpouse(s);

        private static bool IsMixedRealEstateSpouse(this string s) => IsMixedColumn(s) && HasRealEstateStr(s) && HasSpouse(s) && !HasChild(s);

        private static bool IsMixedRealEstate(this string s) =>
            // в этой колонке нет подколонок, все записано на естественном языке
            IsMixedColumn(s) && HasRealEstateStr(s);

        private static bool IsStateRealEstate(this string s) =>
            // в этой колонке нет подколонок, все записано на естественном языке
            IsStateColumn(s) && HasRealEstateStr(s);
        private static bool IsOwnedRealEstate(this string s) =>
            // в этой колонке нет подколонок, все записано на естественном языке
            IsOwnedColumn(s) && HasRealEstateStr(s);

        private static bool IsMixedRealEstateSquare(this string s) => IsMixedColumn(s) && HasSquareString(s);

        private static bool IsMixedRealEstateCountry(this string s) => IsMixedColumn(s) && HasCountryString(s);

        private static bool IsVehicle(this string s)
        {
            var clean = s.OnlyRussianLowercase();
            return (clean.Contains("транспорт") || clean.Contains("трнспорт") || clean.Contains("движимоеимущество")) &&
                    !clean.Contains("источник") && !clean.Contains("недвижимоеимущество");
        }

        private static bool IsVehicleType(this string s)
        {
            var clean = s.OnlyRussianLowercase();
            return (clean.Contains("транспорт") || clean.Contains("трнспорт") || clean.Contains("движимоеимущество")) &&
                    clean.Contains("вид") && !clean.Contains("марк");
        }

        private static bool IsVehicleModel(this string s)
        {
            var clean = s.OnlyRussianLowercase();
            return (clean.Contains("транспорт") || clean.Contains("трнспорт") || clean.Contains("движимоеимущество")) &&
                    clean.Contains("марка") && !clean.Contains("вид");
        }

        private static bool IsDeclaredYearlyIncome(this string str)
        {
            var strLower = str.OnlyRussianLowercase();
            return strLower.Contains("годовойдоход")
                    || strLower.StartsWith("сведенияодоходеза")
                    || strLower.Contains("годовогодохода")
                    || strLower.Contains("суммадохода")
                    || strLower.StartsWith("доход")
                    || strLower.Contains("суммадоходов")
                    || strLower.Contains("декларированныйдоход")
                    || strLower.Contains("декларированныйгодовой")
                    || strLower.Contains("декларированногодохода")
                    || strLower.Contains("декларированногогодовогодоход")
                    || strLower.Contains("общаясуммадохода")
                   ;
        }

        private static bool IsMainWorkPositionIncome(this string str) => Regex.Match(str, @"сумма.*месту\s+работы").Success;

        private static bool IsDeclaredYearlyIncomeThousands(this string s) => s.IsDeclaredYearlyIncome() && s.Contains("тыс.");

        private static bool IsDataSources(this string s) => s.Contains("сведен");

        private static bool IsComments(this string s)
        {
            var lowerS = s.OnlyRussianLowercase();
            return lowerS.Contains("примечани");
        }

        private static bool IsAcquiredProperty(this string s)
        {
            var strLower = s.OnlyRussianLowercase();
            return strLower.Contains("приобретенногоимущества");
        }

        private static bool IsMoneySources(this string s)
        {
            var strLower = s.OnlyRussianLowercase();
            return strLower.Contains("источникполучениясредств") || strLower.Contains("сточникиполучениясредств");
        }

        private static bool IsTransactionSubject(this string s)
        {
            var strLower = s.OnlyRussianLowercase();
            return strLower.Contains("предметсделки");
        }
        private static bool IsMoneyOnBankAccounts(this string s)
        {
            var strLower = s.OnlyRussianLowercase();
            return strLower.Contains("денежныесредства") && (
                strLower.Contains("банках") || strLower.Contains("вкладах"));
        }

        private static bool IsSecuritiesField(this string s)
        {
            var strLower = s.OnlyRussianLowercase();
            return strLower.Contains("ценныебумаги");
        }

        private static bool IsSpendingsField(this string s)
        {
            var strLower = s.OnlyRussianLowercase();
            return strLower.Contains("расход") && !strLower.Contains("доход");
        }

        private static bool IsStocksField(this string s)
        {
            var strLower = s.OnlyRussianLowercase();
            return strLower.Contains("участие") && strLower.Contains("организациях");
        }
        private static bool IsMixedLandAreaSquare(this string s)
        {
            var strLower = s.OnlyRussianLowercase();
            return strLower.Contains("земельныеучастки") && strLower.Contains("квм");
        }
        private static bool IsMixedLivingHouseSquare(this string s)
        {
            var strLower = s.OnlyRussianLowercase();
            return strLower.Contains("жилыедома") && strLower.Contains("квм");
        }
        private static bool IsMixedAppartmentSquare(this string s)
        {
            var strLower = s.OnlyRussianLowercase();
            return strLower.Contains("квартиры") && strLower.Contains("квм");
        }
        private static bool IsMixedSummerHouseSquare(this string s)
        {
            var strLower = s.OnlyRussianLowercase();
            return strLower.Contains("дачи") && strLower.Contains("квм");
        }
        private static bool IsMixedGarageSquare(this string s)
        {
            var strLower = s.OnlyRussianLowercase();
            return strLower.Contains("гаражи") && strLower.Contains("квм");
        }
    }
}
