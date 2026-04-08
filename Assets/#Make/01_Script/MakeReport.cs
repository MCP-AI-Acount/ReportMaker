using System.Text;
using UnityEngine;

public class MakeReport : MonoBehaviour
{
    public static string BuildReportText(UserSetting.ReportInfo report)
    {
        if (report == null)
        {
            return "보고서 데이터를 생성할 수 없습니다.";
        }

        var builder = new StringBuilder();
        builder.AppendLine("===== 보고서 =====");
        builder.AppendLine();

        if (!string.IsNullOrEmpty(report.CustomerName))
        {
            builder.AppendLine($"고객명: {report.CustomerName}");
        }

        if (!string.IsNullOrEmpty(report.CustomerId))
        {
            builder.AppendLine($"고객 ID: {report.CustomerId}");
        }

        if (!string.IsNullOrEmpty(report.BillingPeriod))
        {
            builder.AppendLine($"청구 기간: {report.BillingPeriod}");
        }

        builder.AppendLine();
        builder.AppendLine("--- 상품 정보 ---");

        if (report.Products.Count == 0)
        {
            builder.AppendLine("상품 정보를 추출하지 못했습니다.");
        }
        else
        {
            builder.AppendLine("상품명\t\t금액");
            foreach (var product in report.Products)
            {
                builder.AppendLine($"{product.Name}\t\t{product.AmountText}원");
            }
        }

        builder.AppendLine();
        builder.AppendLine("--- 합계 ---");

        if (!string.IsNullOrEmpty(report.TotalAmountText))
        {
            builder.AppendLine($"총액: {report.TotalAmountText}원");
        }
        else
        {
            builder.AppendLine($"총액: {report.TotalAmount:N0}원");
        }

        builder.AppendLine();
        builder.AppendLine("--- 원본 텍스트 ---");
        builder.AppendLine(report.RawText ?? "");

        return builder.ToString();
    }
}
