using System.IO;
using UnityEngine;

public class ProcessManager : MonoBehaviour
{
    /*

    1. PDF리더로 읽기 : GetSetPDF.cs
    2. PDF에서 텍스트 추출하기 : GetSetPDF.cs
    3. 텍스트에서 필요한 정보 추출하기 : GetSetPDF.cs, UserSetting.cs
     - 개인 / 상품 정보: 소유 상품, 상품별 금액, 총액, 지난 기간 / 전체기간 등
    4. 추출한 정보를 바탕으로 보고서 작성하기 : MakeReport.cs
     - 보고서 양식: 샘플로 제시된 양식 참고
    5. 작성된 보고서를 PDF로 저장하기 : GetSetPDF.cs (PDF 생성 기능 활용)

    */

    [Header("PDF 입력/출력")]
    public string inputPdfPath = "InputPDFs/sample.pdf";
    public string outputPdfPath = "Outputs/Report.pdf";
    public bool runOnStart = false;

    void Start()
    {
        if (runOnStart)
        {
            ProcessPdf();
        }
    }

    public void ProcessPdf()
    {
        if (GetSetPDF.ProcessReport(inputPdfPath, outputPdfPath, out string errorMessage))
        {
            string fullOutputPath = GetSetPDF.GetStreamingAssetsPath(outputPdfPath);
            Debug.Log($"보고서 생성 완료. PDF: {fullOutputPath}");
        }
        else
        {
            Debug.LogError(errorMessage);
        }
    }
}
