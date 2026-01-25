using UnityEngine;
using TMPro;

public class SimpleResultUI : MonoBehaviour
{
    [Header("--- UI References ---")]
    public TextMeshProUGUI txtGameType;   // اربطه بـ "points text"
    public TextMeshProUGUI txtTotalUS;    // اربطه بـ "total US"
    public TextMeshProUGUI txtTotalThem;  // اربطه بـ "total them"

    // الدالة تستقبل كل التفاصيل لكن تعرض المجموع فقط
    public void ShowSimpleResult(string type, int usScore, int themScore, int usGround, int themGround, int usProj, int themProj)
    {
        // 1. عرض نوع اللعبة (صن / حكم)
        if (txtGameType) txtGameType.text = type;

        // 2. جمع كل النقاط (أبناط + أرض + مشاريع)
        int finalUs = usScore + usGround + usProj;
        int finalThem = themScore + themGround + themProj;

        // 3. عرض المجموع النهائي فقط
        if (txtTotalUS) txtTotalUS.text = finalUs.ToString();
        if (txtTotalThem) txtTotalThem.text = finalThem.ToString();

        // 4. إظهار الواجهة
        gameObject.SetActive(true);
    }
}