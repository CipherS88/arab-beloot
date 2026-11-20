<div align="center">

# ♠️ Arab-Beloot Engine

### *A state machine and a heuristic brain for competitive Baloot*

![C%23](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![Unity](https://img.shields.io/badge/Unity-000000?style=for-the-badge&logo=unity&logoColor=white)
![Docs](https://img.shields.io/badge/Docs-Live-0A84FF?style=for-the-badge&logo=githubpages&logoColor=white)

> *Turn logic, Sun/Hokom betting rules, scoring, and bots that actually think about their hand.*

</div>

---

🚀 **[Full documentation →](https://ciphers88.github.io/arab-beloot)**

---

## ✨ عن المشروع

هذا المستودع يحتوي سكربتات C# الأساسية لمشروع البلوت على Unity. البنية مصمّمة لتحمّل حالات لعب معقدة، قوانين الشراء (صن/حكم)، واتخاذ قرار ذكي للبوتات.

- **حالة اللعبة كآلة:** كل دور يمر بمراحل واضحة يمكن تتبّعها واختبارها.
- **قرار البوت احتمالي:** يقيس قوة اليد والاحتمالات قبل اختيار الكرت.
- **فصل المنطق عن العرض:** المحرك لا يعرف شيئًا عن الأنيميشن أو الواجهة.

---

## 📂 الملفات

| الملف | الوظيفة |
| :--- | :--- |
| **`GameManager.cs`** | الـ singleton المركزي: حلقة اللعب، ترتيب الأدوار، وحساب النقاط. |
| **`BotBrain.cs`** | حاوية منطق ثابتة: تحلّل احتمالات اليد وتختار أفضل كرت. |
| **`Card.cs`** | تمثيل الكرت (Rank/Suit) وحركات Lerp البصرية. |
| **`PlayerController.cs`** | الصنف الأساسي المجرّد الذي يحدد عقد اللاعب البشري والآلي. |
| **`HumanPlayer.cs` / `BotPlayer.cs`** | تنفيذ العقد للاعب الحقيقي والبوت. |
| **`GameResultUI.cs` / `SimpleResult*.cs`** | شاشات النتيجة النهائية. |

---

## 🧠 كيف يعمل

```text
Deal → Bidding (Sun / Hokom) → Trick loop → Score
          │                        │
          └── BotBrain decides     └── GameManager validates the play
```

1. **التوزيع والشراء:** `GameManager` يحدد الموزّع ويجمع قرارات الشراء.
2. **اللعب:** كل دور يطلب كرتًا من `PlayerController` — بشريًا أو من `BotBrain`.
3. **الحساب:** تُجمع النقاط حسب نوع الشراء (صن/حكم) وتُعرض في شاشة النتيجة.

---

## 🚀 الاستخدام

هذه سكربتات محرك، ليست لعبة كاملة:

1. أنشئ مشروع Unity 2D/3D جديد.
2. انسخ ملفات `.cs` إلى `Assets/Scripts/`.
3. أضف `GameManager` إلى كائن فارغ في المشهد واربط المراجع من الـ Inspector.
4. شغّل المشهد — سيبدأ التوزيع ويطلب القرارات.

---

<div align="center">

<sub>Built by <b>Abdullah Aljuaid</b> · <a href="https://github.com/CipherS88">@CipherS88</a> · Unity</sub>

</div>
