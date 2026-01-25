using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameResultUI : MonoBehaviour
{
    [Header("--- Header ---")]
    public TextMeshProUGUI txtGameType; // يكتب "صن" أو "حكم"
    public GameObject winBanner; // بانر "فزت"
    public GameObject loseBanner; // بانر "خسرت"

    [Header("--- Team A (Us) Columns ---")]
    public TextMeshProUGUI txtTricksA;
    public TextMeshProUGUI txtGroundA;
    public TextMeshProUGUI txtProjectA;
    public TextMeshProUGUI txtTotalA;
    public TextMeshProUGUI txtFinalResultA; // النتيجة النهائية (مثلاً 26)

    [Header("--- Team B (Them) Columns ---")]
    public TextMeshProUGUI txtTricksB;
    public TextMeshProUGUI txtGroundB;
    public TextMeshProUGUI txtProjectB;
    public TextMeshProUGUI txtTotalB;
    public TextMeshProUGUI txtFinalResultB;

    // دالة لاستقبال البيانات وعرضها
    public void ShowResult(string type, int trickA, int trickB, int groundA, int groundB, int projA, int projB)
    {
        // 1. عرض النوع
        txtGameType.text = type;

        // 2. عرض الأكلات
        txtTricksA.text = trickA.ToString();
        txtTricksB.text = trickB.ToString();

        // 3. عرض الأرض (10)
        txtGroundA.text = groundA.ToString();
        txtGroundB.text = groundB.ToString();

        // 4. عرض المشاريع
        txtProjectA.text = projA.ToString();
        txtProjectB.text = projB.ToString();

        // 5. حساب المجموع (النقاط)
        int totalA = trickA + groundA + projA;
        int totalB = trickB + groundB + projB;

        txtTotalA.text = totalA.ToString();
        txtTotalB.text = totalB.ToString();

        // 6. حساب النتيجة النهائية (القسمة والتقريب حسب قوانين البلوت)
        // (هذا لوجيك مبسط: في الصن نقسم على 5 ونقرب، في الحكم نقسم على 10)
        // يمكنك تعديل المعادلة حسب رغبتك (هل هو دبل؟ هل فيه كبوت؟)
        int finalA = CalculateFinalScore(totalA, type);
        int finalB = CalculateFinalScore(totalB, type);

        txtFinalResultA.text = finalA.ToString();
        txtFinalResultB.text = finalB.ToString();

        // 7. تحديد الفائز والخاسر
        if (totalA >= totalB)
        {
            if(winBanner) winBanner.SetActive(true);
            if(loseBanner) loseBanner.SetActive(false);
        }
        else
        {
            if(winBanner) winBanner.SetActive(false);
            if(loseBanner) loseBanner.SetActive(true);
        }

        gameObject.SetActive(true); // إظهار القائمة
    }

    int CalculateFinalScore(int rawScore, string type)
    {
        // معادلة تقريبية للبلوت
        // في الصن: المجموع 130 (26). في الحكم: المجموع 152 (15)
        if (type == "sun")
        {
            // معادلة الصن: نقرب لأقرب 5 ثم نضرب في 2 (أو نقسم على 5 حسب نظامك)
            // للتبسيط سنعرض الرقم كما هو أو القسمة التقليدية
            return Mathf.RoundToInt(rawScore / 5f); 
        }
        else
        {
            // معادلة الحكم: القسمة على 10 والتقريب
            int score = rawScore / 10;
            int remainder = rawScore % 10;
            if (remainder >= 6) score++; // تجبير الكسر
            return score;
        }
    }
}