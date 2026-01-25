# ♠️ Arab-Beloot Engine

### The Core Logic & AI Architecture
**A robust C# state machine and heuristic AI for the competitive card game, Baloot.**

---

## 🚀 [CLICK HERE TO VIEW FULL DOCUMENTATION]([https://ciphers88.github.io/arab-beloot/](https://ciphers88.github.io/arab-beloot/))
**👆 اضغط هنا لمشاهدة التوثيق الفني الكامل للمشروع**

---

### 📂 Repository Overview
This repository hosts the core C# scripts used in the Unity project. The architecture is designed to handle complex game states, betting rules (Sun/Hokom), and smart bot decision-making.

| File | Description |
| :--- | :--- |
| **GameManager.cs** | The central singleton managing the game loop, turn logic, and score calculation. |
| **BotBrain.cs** | A static logic container. Analyzes hand probability and determines the best card to play. |
| **Card.cs** | Handles the visual representation, Lerp animations, and card data (Rank/Suit). |
| **PlayerController.cs** | Abstract base class defining the contract for both Human and AI players. |

### 🧠 AI Logic (BotBrain)
The AI does not cheat. It uses a priority-based heuristic system:
1.  **Winning Projects:** Checks for 400 (Aces) or 100 sets.
2.  **Hand Strength:** Calculates "Sun" points to determine buying power.
3.  **Hokom Strategy:** Analyzes specific combinations (Jack + 9 + Ace) before calling a suit.

---
*Developed by CipherS88 • Built for Unity*
