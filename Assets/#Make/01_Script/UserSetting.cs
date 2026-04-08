using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

public class UserSetting : MonoBehaviour
{
    [Serializable]
    public class ProductInfo
    {
        public string Name { get; set; }
        public decimal Amount { get; set; }
        public string AmountText { get; set; }
    }

    [Serializable]
    public class ReportInfo
    {
        public string CustomerName { get; set; }
        public string CustomerId { get; set; }
        public string BillingPeriod { get; set; }
        public decimal TotalAmount { get; set; }
        public string TotalAmountText { get; set; }
        public List<ProductInfo> Products { get; set; } = new List<ProductInfo>();
        public string RawText { get; set; }
    }

    [Serializable]
    public class ParseRegexPatterns
    {
        [Tooltip("한 줄: 상품명 + 금액(천단위 콤마). 그룹1=이름, 그룹2=숫자")]
        [TextArea(2, 4)]
        public string lineItem = @"^(.+?)\s+([0-9]{1,3}(?:,[0-9]{3})*)(?:원|W|KRW)?$";

        [Tooltip("총액 줄. 그룹2=숫자 부분")]
        [TextArea(2, 4)]
        public string totalAmount = @"(총\s*액|합계|총 금액)\s*[:：]?\s*([0-9]{1,3}(?:,[0-9]{3})*)(?:원|W|KRW)?";

        [Tooltip("기간(연월일~연월일). 그룹2,3=각 날짜")]
        [TextArea(2, 4)]
        public string billingPeriodFull = @"(기간|청구기간|계약기간)\s*[:：]?\s*([0-9]{4}[./][0-9]{1,2}[./][0-9]{1,2})\s*[-~–]\s*([0-9]{4}[./][0-9]{1,2}[./][0-9]{1,2})";

        [Tooltip("기간(연월~연월). 그룹2,3=각 부분")]
        [TextArea(2, 4)]
        public string billingPeriodShort = @"(기간|청구기간|계약기간)\s*[:：]?\s*([0-9]{4}[./][0-9]{1,2})\s*[-~–]\s*([0-9]{4}[./][0-9]{1,2})";

        [Tooltip("고객 이름. 그룹2=값")]
        [TextArea(1, 3)]
        public string customerName = @"(이름|성명|고객명|회원명)\s*[:：]?\s*(.+)$";

        [Tooltip("고객 번호. 그룹2=값")]
        [TextArea(1, 3)]
        public string customerId = @"(고객번호|회원번호|ID|아이디)\s*[:：]?\s*(.+)$";
    }

    [Header("텍스트 파싱 (정규식)")]
    [Tooltip("인스펙터에서 수정한 패턴은 TryParse / 샘플 UI에 반영됩니다. 정적 TryParseReport()는 코드 기본값을 씁니다.")]
    public ParseRegexPatterns parsePatterns = new ParseRegexPatterns();

    public bool TryParse(string rawText, out ReportInfo report, out string errorMessage)
    {
        return TryParseReport(rawText, parsePatterns, out report, out errorMessage);
    }

    public static bool TryParseReport(string rawText, out ReportInfo report, out string errorMessage)
    {
        return TryParseReport(rawText, new ParseRegexPatterns(), out report, out errorMessage);
    }

    public static bool TryParseReport(string rawText, ParseRegexPatterns patterns, out ReportInfo report, out string errorMessage)
    {
        report = null;
        errorMessage = null;

        if (patterns == null)
        {
            patterns = new ParseRegexPatterns();
        }

        Regex itemPattern;
        Regex totalPattern;
        Regex periodPattern;
        Regex simplePeriodPattern;
        Regex namePattern;
        Regex idPattern;

        try
        {
            itemPattern = new Regex(patterns.lineItem, RegexOptions.IgnoreCase);
            totalPattern = new Regex(patterns.totalAmount, RegexOptions.IgnoreCase);
            periodPattern = new Regex(patterns.billingPeriodFull, RegexOptions.IgnoreCase);
            simplePeriodPattern = new Regex(patterns.billingPeriodShort, RegexOptions.IgnoreCase);
            namePattern = new Regex(patterns.customerName, RegexOptions.IgnoreCase);
            idPattern = new Regex(patterns.customerId, RegexOptions.IgnoreCase);
        }
        catch (ArgumentException ex)
        {
            errorMessage = "정규식 오류: " + ex.Message;
            return false;
        }

        if (string.IsNullOrWhiteSpace(rawText))
        {
            errorMessage = "입력 텍스트가 없습니다.";
            return false;
        }

        report = new ReportInfo { RawText = rawText };
        string[] lines = rawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            if (string.IsNullOrEmpty(report.CustomerName))
            {
                var nameMatch = namePattern.Match(line);
                if (nameMatch.Success)
                {
                    report.CustomerName = nameMatch.Groups[2].Value.Trim();
                    continue;
                }
            }

            if (string.IsNullOrEmpty(report.CustomerId))
            {
                var idMatch = idPattern.Match(line);
                if (idMatch.Success)
                {
                    report.CustomerId = idMatch.Groups[2].Value.Trim();
                    continue;
                }
            }

            if (string.IsNullOrEmpty(report.BillingPeriod))
            {
                var periodMatch = periodPattern.Match(line);
                if (!periodMatch.Success)
                {
                    periodMatch = simplePeriodPattern.Match(line);
                }

                if (periodMatch.Success)
                {
                    report.BillingPeriod = periodMatch.Groups[2].Value.Trim() + " ~ " + periodMatch.Groups[3].Value.Trim();
                    continue;
                }
            }

            if (string.IsNullOrEmpty(report.TotalAmountText))
            {
                var totalMatch = totalPattern.Match(line);
                if (totalMatch.Success)
                {
                    report.TotalAmountText = totalMatch.Groups[2].Value.Trim();
                    if (TryParseAmount(report.TotalAmountText, out decimal total))
                    {
                        report.TotalAmount = total;
                    }
                    continue;
                }
            }

            var itemMatch = itemPattern.Match(line);
            if (itemMatch.Success)
            {
                string product = itemMatch.Groups[1].Value.Trim();
                string amountText = itemMatch.Groups[2].Value.Trim();
                if (TryParseAmount(amountText, out decimal amount))
                {
                    report.Products.Add(new ProductInfo
                    {
                        Name = product,
                        AmountText = amountText,
                        Amount = amount
                    });
                }
                else
                {
                    report.Products.Add(new ProductInfo
                    {
                        Name = product,
                        AmountText = amountText,
                        Amount = 0m
                    });
                }
            }
        }

        if (string.IsNullOrEmpty(report.CustomerName) && string.IsNullOrEmpty(report.CustomerId) && report.Products.Count == 0)
        {
            errorMessage = "보고서에서 개인 정보 또는 상품 정보를 추출할 수 없습니다.";
            return false;
        }

        if (string.IsNullOrEmpty(report.TotalAmountText) && report.Products.Count > 0)
        {
            decimal sum = 0m;
            foreach (var product in report.Products)
            {
                sum += product.Amount;
            }

            report.TotalAmount = sum;
            report.TotalAmountText = sum.ToString("N0", CultureInfo.InvariantCulture);
        }

        return true;
    }

    private static bool TryParseAmount(string text, out decimal amount)
    {
        amount = 0m;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string normalized = text.Replace(",", string.Empty).Replace("원", string.Empty).Replace("W", string.Empty).Replace("KRW", string.Empty).Trim();
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }
}
