using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

public class GetSetPDF : MonoBehaviour
{
    public static bool TryReadAllText(string pdfFilePath, out string fullText, out string errorMessage)
    {
        fullText = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(pdfFilePath))
        {
            errorMessage = "PDF file path is empty.";
            return false;
        }

        if (!File.Exists(pdfFilePath))
        {
            errorMessage = "PDF file not found: " + pdfFilePath;
            return false;
        }

        try
        {
            var sb = new StringBuilder();
            using (PdfDocument document = PdfDocument.Open(pdfFilePath))
            {
                foreach (Page page in document.GetPages())
                {
                    sb.AppendLine(page.Text);
                }
            }

            fullText = sb.ToString();
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    public static bool TryReadPages(string pdfFilePath, out List<string> pageTexts, out string errorMessage)
    {
        pageTexts = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(pdfFilePath))
        {
            errorMessage = "PDF file path is empty.";
            return false;
        }

        if (!File.Exists(pdfFilePath))
        {
            errorMessage = "PDF file not found: " + pdfFilePath;
            return false;
        }

        try
        {
            var list = new List<string>();
            using (PdfDocument document = PdfDocument.Open(pdfFilePath))
            {
                foreach (Page page in document.GetPages())
                {
                    list.Add(page.Text ?? string.Empty);
                }
            }

            pageTexts = list;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    public static bool TryWriteReportPdf(string outputPdfPath, string reportText, out string errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(outputPdfPath))
        {
            errorMessage = "PDF output path is empty.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(reportText))
        {
            errorMessage = "Report text is empty.";
            return false;
        }

        try
        {
            string directory = Path.GetDirectoryName(outputPdfPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            byte[] pdfBytes = CreatePdfDocumentFromText(reportText);
            File.WriteAllBytes(outputPdfPath, pdfBytes);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    private static byte[] CreatePdfDocumentFromText(string reportText)
    {
        using var ms = new MemoryStream();
        
        void WriteBytes(byte[] bytes)
        {
            ms.Write(bytes, 0, bytes.Length);
        }
        
        string WriteAscii(string text)
        {
            WriteBytes(Encoding.ASCII.GetBytes(text));
            return text;
        }

        var offsets = new List<long>();
        
        WriteAscii("%PDF-1.4\n%âãÏÓ\n");

        offsets.Add(ms.Position);
        WriteAscii("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

        offsets.Add(ms.Position);
        WriteAscii("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");

        offsets.Add(ms.Position);
        WriteAscii("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>\nendobj\n");

        offsets.Add(ms.Position);
        WriteAscii("4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");

        string contentStream = GenerateContentStream(reportText);
        byte[] contentBytes = Encoding.ASCII.GetBytes(contentStream);
        offsets.Add(ms.Position);
        WriteAscii($"5 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n");
        WriteBytes(contentBytes);
        WriteAscii("\nendstream\nendobj\n");

        long xrefPosition = ms.Position;
        WriteAscii($"xref\n0 {offsets.Count + 1}\n0000000000 65535 f \n");
        foreach (long offset in offsets)
        {
            WriteAscii(offset.ToString("D10") + " 00000 n \n");
        }

        WriteAscii("trailer\n<< /Size " + (offsets.Count + 1) + " /Root 1 0 R >>\nstartxref\n");
        WriteAscii(xrefPosition.ToString());
        WriteAscii("\n%%EOF");

        return ms.ToArray();
    }

    private static string GenerateContentStream(string reportText)
    {
        var sb = new StringBuilder();
        sb.Append("BT\n");
        sb.Append("/F1 12 Tf\n");
        sb.Append("50 750 Td\n");

        var lines = reportText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        foreach (var line in lines)
        {
            string escapedLine = EscapePdfString(line);
            if (string.IsNullOrEmpty(escapedLine))
            {
                sb.Append("0 -15 Td\n");
            }
            else
            {
                sb.Append($"({escapedLine}) Tj\n");
                sb.Append("0 -15 Td\n");
            }
        }

        sb.Append("ET\n");
        return sb.ToString();
    }

    private static string EscapePdfString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var sb = new StringBuilder();
        foreach (char c in input)
        {
            if (c == '(' || c == ')')
            {
                sb.Append("\\");
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    public static string GetStreamingAssetsPath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        string normalized = path.Replace("\\", "/");
        
        // StreamingAssets 폴더 경로 처리
        if (normalized.StartsWith("InputPDFs/") || normalized.StartsWith("Backgrounds/") || normalized.StartsWith("Outputs/"))
        {
            return Path.Combine(Application.streamingAssetsPath, normalized);
        }
        
        // 기존 Assets 폴더 처리
        if (normalized.StartsWith("Assets/"))
        {
            normalized = normalized.Substring("Assets/".Length);
        }

        return Path.Combine(Application.dataPath, normalized);
    }

    public static bool ProcessReport(string inputPdfPath, string outputPdfPath, out string errorMessage)
    {
        errorMessage = null;

        string fullInputPath = GetStreamingAssetsPath(inputPdfPath);
        string fullOutputPath = GetStreamingAssetsPath(outputPdfPath);

        if (!TryReadAllText(fullInputPath, out string pdfText, out string readError))
        {
            errorMessage = $"PDF 읽기 실패: {readError}";
            return false;
        }

        if (!UserSetting.TryParseReport(pdfText, out UserSetting.ReportInfo reportInfo, out string parseError))
        {
            errorMessage = $"텍스트 파싱 실패: {parseError}";
            return false;
        }

        string reportText = MakeReport.BuildReportText(reportInfo);

        if (!TryWriteReportPdf(fullOutputPath, reportText, out string writeError))
        {
            errorMessage = $"PDF 저장 실패: {writeError}";
            return false;
        }

        string debugTextPath = Path.ChangeExtension(fullOutputPath, ".txt");
        File.WriteAllText(debugTextPath, reportText);

        return true;
    }
}
