# KẾ HOẠCH CHI TIẾT 4 GIAI ĐOẠN TRIỂN KHAI CODE (PIXEL SURVIVOR v2.1)

> **Tài liệu nguồn:** `PixelSurvivor_Core_Game_Design_v2.1_Patched.md`  
> **Trạng thái:** 💾 **ĐÃ LƯU & KHÓA VÀO HỆ THỐNG**  
> **Mục đích:** Danh sách kiểm tra (Checklist) chi tiết từng lớp (class), tham số (parameter), công thức và tiêu chí kiểm thử cho 4 giai đoạn code.

---

## 🗺️ SƠ ĐỒ LỘ TRÌNH THỰC HIỆN (4 PHASES)

```
[ Giai đoạn 1: Nền tảng & Chỉ số lõi ]
  ├── TimeScaleManager (Quản lý pause tập trung, PauseToken)
  ├── PlayerStats (Hard Caps: Armor 35%, CDR 30%, Speed +40%, Crit 60%, Dmg +60%)
  ├── Global Contact I-Frame (0.35s + cộng dồn bị vây +25%/con) & Ngắt hồi máu 1.0s
  └── ExperienceManager (Đường cong EXP mới 20 * Lv^1.20, mục tiêu Lv 46-47 lúc 15:00)
       │
       ▼
[ Giai đoạn 2: Vũ khí, Passives & Tiến hóa ]
  ├── Cân bằng 6 Vũ khí theo ngân sách B(L) (Daggers 171 DPS, Crossbow 87.5 DPS)
  ├── Ưu tiên mục tiêu (Boss > Elite > Quái gần) cho Daggers & Crossbow
  ├── Hệ thống 9 Passives (Lv 1 -> 5, giữ slot khi Lv5, Evolution Pity, Thẻ Swap)
  ├── Bể thẻ dự phòng (Hearty Meal, Emerald Pouch, Scholar's Tome, Bounty Mark)
  └── Ma trận 6 Vũ khí Tiến hóa (Khóa mở từ >= 09:00, tối đa 2 món/ván)
       │
       ▼
[ Giai đoạn 3: Quái vật, Bầy đàn & Elite Budget ]
  ├── Công thức HP Final Surge & Sát thương chạm tăng theo thời gian (1 + 0.05t)
  ├── Chasers tăng tốc nhẹ (+0.02 m/s/phút), kiểm soát trần Cap toàn cục (150/180/220)
  ├── Triệt tiêu exploit: Quái triệu hồi (Vex, Slime con, Boss minions) rơi 0 EXP & 0 💎
  ├── Elite Budget Spawner (Roll 2s, cap 2/4/6, 5 affixes cân bằng)
  └── Sự kiện Pincer (1:30, bán kính 4.5m, telegraph 1.5s, góc thoát 60°)
       │
       ▼
[ Giai đoạn 4: Bosses, Kinh tế & Hoàn thiện ]
  ├── Mini-Boss Nether Golem I (5:00 - 3,300 HP) & Golem II (10:00 - 9,600 HP, vệt lửa)
  ├── Final Boss The Warden (15:00 - 42,000 HP, Sonic Boom, Charge, Sculk Tendril Pull, Enrage)
  ├── Lucky Block 3 hits (25% EXP cấp kế) & TNT 12m cố định (Capped EXP & 💎)
  ├── Bảng giá Reroll Emerald [2, 3, 5, 8, 12, 18 💎] & Tự gộp ngọc (>250 gem)
  └── UI Chọn độ khó 3 chế độ (Normal, Hard, Hardcore)
```

---

## 📋 CHI TIẾT TỪNG GIAI ĐOẠN

### 🟡 GIAI ĐOẠN 1: NỀN TẢNG ENGINE & CHỈ SỐ LÕI (CORE STATS)
*Mục tiêu: Đảm bảo game chạy mượt, không lỗi pause ngầm, chỉ số người chơi được kiểm soát trong trần toán học.*

* [x] **1.1. Tạo `TimeScaleManager.cs`:** (ĐÃ HOÀN THÀNH)
  - Quản lý `Time.timeScale` duy nhất trong game bằng cơ chế `PauseToken` / `pauseLocks`.
  - Hỗ trợ hàm `AcquirePause(string source)` và `ReleasePause(string source)`.
  - Rà soát xóa toàn bộ các dòng gán `Time.timeScale` rải rác trong `Awake()` của các script khác.
* [x] **1.2. Nâng cấp `PlayerStats.cs` & Quản lý Hard Caps:** (ĐÃ HOÀN THÀNH)
  - Trần Armor: $\le 35\%$.
  - Trần Cooldown Reduction (CDR): $\le 30\%$ (Sàn CD Twin Daggers: $0.25s$).
  - Trần Bonus Tốc độ di chuyển: $\le +40\%$.
  - Trần Crit Chance: $\le 60\%$.
  - Trần Bonus Damage: $\le +60\%$.
* [x] **1.3. Hệ thống Sát thương Chạm & I-Frame Toàn Cục:** (ĐÃ HOÀN THÀNH)
  - I-frame chạm toàn cục: $0.35s$.
  - Công thức cộng dồn khi bị vây: $\text{DMG} \times (1 + 0.25 \times \min(N_{\text{extra}}, 4))$.
  - Khóa riêng biệt $0.25s$ cho nguồn nổ, độc, đạn quái, đòn Boss.
  - Ngắt hồi máu (Regen pause): Mọi nguồn hồi phục tạm dừng $1.0s$ sau khi người chơi bị đánh trúng.
* [x] **1.4. Cập nhật Đường cong EXP (`ExperienceManager.cs`):** (ĐÃ HOÀN THÀNH)
  - Công thức: $\text{EXP}(Lv) = \text{Round}(20 \times Lv^{1.20})$.
  - Hệ số EXP theo mode: Normal $1.00$, Hard $1.10$, Hardcore $1.25$.
  - Benchmark: Mốc 15:00 đạt khoảng cấp 46–47 (45–46 lượt chọn thẻ).

---

### ⚔️ GIAI ĐOẠN 2: CÂN BẰNG VŨ KHÍ, PASSIVES & TIẾN HÓA
*Mục tiêu: Đưa sát thương người chơi về mức cân bằng B(L), triển khai trọn vẹn 9 passives và 6 vũ khí tiến hóa.*

* [x] **2.1. Cân bằng 6 Vũ khí theo Ngân sách $B(L)$:**
  - `Magma Broadsword`: Lv8 = 62 dmg, CD 0.8s, Burn 10 dmg/s trong 3s (tổng DPS = 87.5).
  - `Battle Axe`: Lv8 = 100 dmg, CD 1.25s, Stagger 0.4s, Knockback 5.5 (tổng DPS = 80.0).
  - `Twin Daggers`: Sửa triệt để lỗi 928 DPS ➔ Lv8 = 18x4 dmg, CD 0.42s, Crit +8% điểm phần trăm (tổng DPS = 171.4).
  - `Dark Scythe`: Lv8 = 67 dmg, CD 0.95s, Hồi 1.5 HP/kill, trần hồi 5 HP/s (tổng DPS = 70.5).
  - `Crystal Laser`: Lv8 = 92 dmg, CD 1.10s, tia xuyên thẳng (tổng DPS = 83.6).
  - `Crossbow`: Sửa triệt để lỗi 630 DPS ➔ Lv8 = 3x17.5 dmg, CD 0.60s, xuyên 3 với -25%/lần xuyên (tổng DPS = 87.5).
* [x] **2.2. Ưu tiên mục tiêu vũ khí:**
  - Twin Daggers & Crossbow ưu tiên: $\text{Boss} > \text{Elite} > \text{Quái gần nhất}$.
* [x] **2.3. Triển khai 9 Passive Items (Lv 1 ➔ Lv 5):**
  - Cài đặt 4 slot passives (`MaxPassives = 4`).
  - Cài đặt 9 passives theo bảng chỉ số v2.1.
  - Quy tắc: Đạt Lv 5 biến mất khỏi pool nhưng **vẫn giữ slot**.
  - Bổ sung **Bể thẻ dự phòng**: Hearty Meal (+40% HP), Emerald Pouch (+25 💎), Scholar's Tome (+150 EXP), Bounty Mark (+10% Boss/Elite dmg).
* [x] **2.4. Khóa Tiến hóa & Evolution Pity:**
  - Khóa tiến hóa: Chỉ kích hoạt từ phút **$\ge 09:00$**, tối đa **2 vũ khí tiến hóa/ván**.
  - Pity từ 08:00: Tăng trọng số x3 và cam kết ra passive cặp trong $\le 3$ lượt, hoặc đưa thẻ **Swap** đổi đồ.
* [x] **2.5. Lập trình 6 Vũ khí Tiến hóa Thần thoại:**
  - *Solar Flare Greatsword*: Nova 3.0m (95 dmg, CD 0.8s) + Aura 12 dmg/s tối đa 12 quái.
  - *Executioner's Greataxe*: Cone 3.6m (150 dmg, CD 1.25s) + Execute quái thường < 10% HP.
  - *Shadow Phantom Daggers*: 8x16 dmg mỗi 0.5s + lướt nhỏ 1.5m.
  - *Reaper's Harvest*: 90 dmg, CD 0.85s + Hút máu 4% (trần 8 HP/s) + Soul Shield 30 HP.
  - *Oblivion Death Ray*: 2 tia rộng 0.8m, xoay 90°/s, trần 15 mục tiêu/tick, hãm xoay còn 30°/s khi quét Boss.
  - *Gatling Repeater*: Quạt 5 mũi (20°), xuyên 4, nổ lan 1.0m.

---

### 🧟 GIAI ĐOẠN 3: BẦY ĐÀN QUÁI VẬT, CAP & TINH ANH (ELITE)
*Mục tiêu: Đảm bảo áp lực đầu trận đến cuối trận luôn căng thẳng, dẹp bỏ nguy cơ lag máy và cày EXP bất tận.*

* [ ] **3.1. Quy chuẩn Máu quái Final Surge & Tốc độ:**
  - Thang HP Final Surge: Tăng thêm $+35\%$ từ phút 12:00 đến 15:00.
  - Chasers (Zombie, Husk, ZombieV2, HuskShovel) tăng tốc $+0.02\text{ m/s/phút}$.
  - Sát thương chạm quái tăng theo thời gian: $\text{DMG} \times (1 + 0.05t)$.
* [ ] **3.2. Quản lý Cap quái toàn cục & Chặn exploit:**
  - Cap toàn cục: Normal 150, Hard 180, Hardcore 220 (gồm cả quái thường, Elite, quái triệu hồi).
  - Sub-cap quái triệu hồi: 25 con.
  - **Triệt tiêu exploit:** Quái triệu hồi (Vex từ Evoker, Slime con, quái Boss) **tuyệt đối không rơi EXP, không rơi Emeralds, không kích hoạt hồi máu**.
* [ ] **3.3. Tối ưu Vật lý Bầy quái:**
  - Tắt va chạm Enemy–Enemy trong Physics 2D matrix.
  - Tách bầy bằng Spatial Grid boids ở tần số 10 Hz.
* [ ] **3.4. Hệ thống Sinh Elite theo Budget:**
  - Roll mỗi 2s: Normal 3% (từ 2:00, cap 2 con), Hard 5% (từ 1:30, cap 4 con), Hardcore 8% (từ 1:00, cap 6 con). Con Elite đầu tiên chắc chắn xuất hiện.
  - 5 Affixes: Swift (+12%), Armored (25%), Berserk (+35% dmg), Regenerating (1%/s), Volatile (nổ 30 dmg).
  - Tỷ lệ rơi rương: 20% / 17% / 15%, pity +10%, trần 10 / 9 / 8 rương/ván.
* [ ] **3.5. Sự kiện Wave đặc biệt:**
  - Sự kiện **Pincer** lúc 1:30: Vòng tròn 4.5m, cảnh báo 1.5s, chừa góc thoát 60° (Hard: 6 Zombie, Hardcore: 8 Zombie).
  - Sự kiện Swarm Rush (4:00 & 7:30), Siege (12:30).

---

### 👑 GIAI ĐOẠN 4: BOSSES, KINH TẾ & GIAO DIỆN CHỌN ĐỘ KHÓ
*Mục tiêu: Trận chiến trùm mãn nhãn, cân bằng kinh tế Emerald và hoàn thiện giao diện trước trận.*

* [ ] **4.1. Mini-Boss Nether Golem I & II:**
  - Golem I (5:00): 3,300 HP, Ground Slam (35 dmg, 3m), Lava Throw 3 viên; Rương thưởng +1 cấp trang bị (không tiến hóa).
  - Golem II (10:00): 9,600 HP, Slam 3.6m (50 dmg), Enrage <50% HP (+20% tốc độ, vệt lửa 12 dmg/s); Rương thưởng đảm bảo tiến hóa.
* [ ] **4.2. Đại Boss The Warden (15:00):**
  - Máu: 42,000 HP (Normal). Giảm 60% tốc độ spawn quái thường.
  - Kỹ năng: Melee Smash (60 dmg), Sonic Boom (45 dmg), Charge (55 dmg, 5.5 m/s).
  - Kỹ năng chống kiting: **Sculk Tendril Pull** (khi người chơi cách > 5.5m, gây 25 dmg và kéo về cự ly 3.0m).
  - Đồng hồ Enrage: 180s / 150s / 130s (+50% dmg, +35% move speed).
* [ ] **4.3. Cân bằng Vật phẩm & Kinh tế:**
  - Golden Lucky Block: 60 HP, 3 hits, thưởng 25% EXP cấp kế (min 5 EXP) + 2–5 💎.
  - Screen Nuke TNT: Bán kính 12.0m cố định, quái thường chết ngay, Elite mất 35% HP, Boss miễn nhiễm; trần thưởng 50% EXP cấp kế và 15 💎; không kích lifesteal; giãn cách tối thiểu 45s.
  - Phí Reroll Emerald: Lần 1 miễn phí, các lần sau [2, 3, 5, 8, 12, 18 💎] (Villager giảm 50%).
  - Tự động gộp ngọc (Auto-merge): Tự động gộp ngọc lân cận khi tổng số gem trên sàn vượt 250.
* [ ] **4.4. UI Chọn Độ Khó (Difficulty Selection Modal):**
  - Giao diện 3 thẻ (Normal, Hard, Hardcore) với các tag chỉ số Risk & Reward trực quan.

---

## 🔒 TRẠNG THÁI KHÓA VÀ CAM KẾT
* Kế hoạch này đã được lưu vào hệ thống tệp tin dự án và Memory Bank của Agent.
* Khi bạn xác nhận, Agent sẽ lần lượt thực hiện từng mục nhỏ theo đúng danh sách kiểm tra trên, đảm bảo kiểm thử và không bỏ sót bất kỳ quy định nào.
