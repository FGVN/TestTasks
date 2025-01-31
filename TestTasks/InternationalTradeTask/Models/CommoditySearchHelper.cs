using System;

namespace TestTasks.InternationalTradeTask.Models
{
    internal static class CommoditySearchHelper
    {
        public static double GetEffectiveTariff(ICommodityGroup root, string targetName, bool getImport)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));

            var commodity = FindCommodity(root, targetName);
            if (commodity == null)
                throw new ArgumentException($"Commodity with name \"{targetName}\" not found.");

            return CalculateEffectiveTariff(root, commodity, getImport);
        }

        private static ICommodityGroup FindCommodity(ICommodityGroup current, string targetName)
        {
            if (current.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                return current;

            if (current.SubGroups != null)
            {
                foreach (var sub in current.SubGroups)
                {
                    var found = FindCommodity(sub, targetName);
                    if (found != null)
                        return found;
                }
            }

            return null;
        }

        private static double CalculateEffectiveTariff(ICommodityGroup root, ICommodityGroup target, bool getImport)
        {
            double? tariff = getImport ? target.ImportTarif : target.ExportTarif;
            ICommodityGroup parent = FindParent(root, target);

            while (parent != null)
            {
                tariff ??= getImport ? parent.ImportTarif : parent.ExportTarif;
                parent = FindParent(root, parent);
            }

            return tariff ?? 0;
        }

        private static ICommodityGroup FindParent(ICommodityGroup current, ICommodityGroup target)
        {
            if (current.SubGroups == null) return null;

            foreach (var sub in current.SubGroups)
            {
                if (sub == target) return current;

                var found = FindParent(sub, target);
                if (found != null) return found;
            }

            return null;
        }
    }
}
