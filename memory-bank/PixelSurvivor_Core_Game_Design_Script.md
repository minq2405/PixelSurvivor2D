# PIXEL SURVIVOR - CORE GAME DESIGN SPEC & SCRIPT (AGENT BLUEPRINT)
> **Tài liệu đặc tả toàn diện lõi Game (Core Game Design Specification & Mechanics)**  
> **Mục tiêu:** Cung cấp đầy đủ toàn bộ kiến trúc lõi, công thức toán học, thông số cân bằng, vòng lặp tiến trình và ràng buộc kỹ thuật để bất kỳ AI Agent hoặc lập trình viên nào đọc qua đều có thể nắm bắt 100% logic game mà không cần tra cứu thêm tài liệu khác.

---

## 1. TỔNG QUAN DỰ ÁN & VÒNG LẶP CỐT LÕI (GAME OVERVIEW & CORE LOOP)

* **Tên dự án:** Pixel Survivor
* **Thể loại:** 2D Top-Down Roguelite Action Survivor (Vampire Survivors loop kết hợp phong cách đồ họa Minecraft).
* **Nền tảng & Engine:** PC & Mobile | Unity 6 LTS (6000.4.10f1) | Universal Render Pipeline 2D (URP 2D).
* **Chuẩn Pixel Art:** Pixels Per Unit (PPU) = **32** trên toàn bộ sprite nhân vật, quái vật, VFX và môi trường.
* **Quy chuẩn Ngôn ngữ In-game:** 100% Tiếng Anh (giao diện, thẻ nâng cấp, thông số, thông báo).
* **Kiến trúc Zero-DB:** Không dùng database cross-run; mọi tài nguyên (EXP, Emeralds, nâng cấp) hoạt động theo từng lượt chơi độc lập (In-Run Session Economy).

### Vòng lặp Gameplay 15 Phút (The 15-Minute Loop):
```
[Di chuyển & Né tránh] ➔ [Tự động tấn công quái trong tầm] ➔ [Nhặt EXP & Emeralds]
       ▲                                                                   │
       │                                                                   ▼
[Tiến hóa Vũ khí / Diệt Boss] ◄─── [Chọn Thẻ Nâng Cấp (4 Vũ khí + 4 Passives)]
```
1. **00:00 - 05:00 (Giai đoạn Đầu):** Kiting quái cơ bản (Zombie, Husk), tích lũy cấp độ, kích hoạt đủ 4 vũ khí và passives.
2. **05:00 & 10:00 (Mini-Boss):** Tiêu diệt Nether Golem để nhận Rương May Mắn Hoàng Kim (Lucky Block Chest).
3. **10:00 - 14:00 (Tối ưu Build):** Đưa vũ khí lên Lv 8 và passives lên Lv 5 để chuẩn bị tiến hóa.
4. **15:00 (Đại chiến Final Boss):** The Warden xuất hiện; hạ gục để hoàn thành màn chơi (Victory).

---

## 2. CÔNG THỨC TOÁN HỌC CỐT LÕI (CORE MATHEMATICAL FORMULAS)

### 2.1. Công thức Máu Quái Vật (Controlled Linear Scaling):
Khắc phục lạm phát máu hàm mũ, quái thường cuối game được kiểm soát tuyến tính trong khoảng 150 - 450 HP:
$$\text{HP}(t) = \text{HP}_{\text{base}} \times (1.0 + 0.25 \times t)$$
*(Trong đó $t \in [0.0, 15.0]$ là thời gian trận đấu tính bằng phút)*.
* Phút 00:00 $\rightarrow \times 1.00$
* Phút 05:00 $\rightarrow \times 2.25$
* Phút 10:00 $\rightarrow \times 3.50$
* Phút 15:00 $\rightarrow \times 4.75$

### 2.2. Công thức Tốc độ Di chuyển Quái (Gentle Late-Game Speed Scaling):
Tốc độ tăng nhẹ theo từng phút để duy trì độ căng thẳng cuối trận mà không biến quái thành bất khả né:
$$\text{Speed}(t) = \text{Speed}_{\text{base}} + (\text{SpeedScalePerMin} \times t)$$
*(SpeedScalePerMin dao động từ $+0.02$ đến $+0.03\text{ m/s mỗi phút}$ tùy loài)*.

### 2.3. Công thức Sát Thương & Chí Mạng (Player Damage & Critical Formula):
$$\text{HitDMG} = \text{DMG}_{\text{weapon}} \times (1.0 + \text{BonusDmgPct})$$
$$\text{FinalDMG} = \begin{cases} \text{HitDMG} \times \text{CritMult} & \text{nếu } \text{Random}(0, 1) < \text{CritChance} \text{ (Số nổ đỏ)} \\ \text{HitDMG} & \text{ngược lại (Số nổ vàng)} \end{cases}$$

### 2.4. Công thức Giảm Sát Thương của Giáp (Armor Reduction):
$$\text{DamageTaken} = \text{EnemyContactDMG} \times (1.0 - \text{ArmorPct})$$

### 2.5. Đường cong Kinh Nghiệm Lên Cấp (EXP Progression Curve):
$$\text{EXP}_{\text{required}}(Lv) = \text{Round}(15 \times Lv^{1.35})$$
*(Lv 1: 15 EXP | Lv 5: 133 EXP | Lv 10: 336 EXP | Lv 20: 860 EXP)*.

---

## 3. THÔNG SỐ 3 NHÂN VẬT KHỞI ĐẦU (PLAYABLE CHARACTERS)

| Chỉ số | **Steve (The Iron Survivor)** | **Alex (The Swift Ranger)** | **Villager (The Fortune Merchant)** |
| :--- | :---: | :---: | :---: |
| **Đặc điểm lối chơi** | Đấu sĩ cận chiến / Đỡ đòn | Xạ thủ tầm xa / Tốc độ cực cao | Kinh tế / Thần Reroll / Hút ngọc |
| **Máu khởi điểm (Max HP)** | **120 HP** | **85 HP** | **100 HP** |
| **Hồi máu tự nhiên (Regen)** | 0.5 HP/s | 0.0 HP/s | 0.2 HP/s |
| **Tốc độ chạy (Move Speed)** | **4.0 m/s** (Chuẩn tham chiếu) | **5.0 m/s** (+25%) | **4.4 m/s** (+10%) |
| **Giảm sát thương (Armor)** | **10%** | 0% | 0% |
| **Tỉ lệ Chí mạng (Crit Chance)** | 5% | **15%** | 8% |
| **Sát thương Chí mạng (Crit Mult)**| 180% | **200%** | 170% |
| **Bán kính Nam Châm (Magnet)** | 1.7 m | 1.7 m | **2.38 m** (+40%) |
| **Vũ khí khởi đầu** | Flaming Broadsword | Twin Daggers | Crossbow |
| **Kỹ năng nội tại** | *Iron Will:* +20% Dmg khi < 50% HP | *Agility:* -15% Cooldown vũ khí | *Merchant Luck:* Reroll giảm 50%, Emerald rơi x3 |

---

## 4. HỆ THỐNG VŨ KHÍ TẤN CÔNG (6 WEAPONS - LV 1 ĐẾN LV 8)

* **Quy chuẩn Slot:** Tối đa **4 Vũ Khí**.
* **Cơ chế tấn công:** Tự động phát hiện mục tiêu `IDamageable` trong phạm vi; chỉ kích hoạt chém/bắn khi có kẻ địch; sprite tác quyền luôn quay về hướng Phải (0°).

| Tên Vũ Khí | Loại hình / Tầm đánh | Lv 1 (Khởi điểm) | Lv 4 (Giữa trận) | Lv 8 (Cực đại) | Cơ chế đặc biệt |
| :--- | :--- | :---: | :---: | :---: | :--- |
| **Magma Broadsword** | Chém vòng cung 160° (2.6m) | 25 Dmg \| CD: 1.10s | 55 Dmg \| CD: 0.90s | **120 Dmg** \| CD: 0.65s | Đẩy lùi 5.0, thiêu đốt 15 dmg/s trong 3s. |
| **Battle Axe** | Bổ chấn động diện rộng (2.8m)| 45 Dmg \| CD: 1.65s | 100 Dmg \| CD: 1.35s | **220 Dmg** \| CD: 0.95s | Đẩy lùi 7.0, nổ Crit cực mạnh 450-650 Dmg! |
| **Twin Daggers** | Đâm liên hoàn mục tiêu gần (1.8m)| 14x2 Dmg \| CD: 0.65s | 32x3 Dmg \| CD: 0.45s | **65x4 Dmg** \| CD: 0.28s | Chém luân phiên 35 FPS, Crit +30%, dồn dame đơn mục tiêu. |
| **Dark Scythe** | Quét lưỡi hái 270° quanh thân (3.0m)| 32 Dmg \| CD: 1.35s | 68 Dmg \| CD: 1.05s | **140 Dmg** \| CD: 0.70s | Dọn dẹp quái vây quanh người, hồi 3 HP khi diệt quái. |
| **Crystal Laser** | Tia laze thẳng xuyên thấu (7.0m)| 22 Dmg \| CD: 1.75s | 50 Dmg \| CD: 1.30s | **110 Dmg** \| CD: 0.85s | Chiếu xuyên mọi kẻ địch theo đường thẳng, không góc chết. |
| **Crossbow** | Bắn chùm đạn ngọc lục bảo (8.0m)| 24 Dmg \| CD: 0.95s | 52x2 Dmg \| CD: 0.75s | **105x3 Dmg** \| CD: 0.50s | Bắn chùm 3 tia, xuyên qua 5 mục tiêu liên tiếp. |

---

## 5. HỆ THỐNG TRANG BỊ BỊ ĐỘNG - PHƯƠNG ÁN 1 (9 PASSIVES - LV 1 ĐẾN LV 5)

* **Quy chuẩn Slot:** Tối đa **4 Trang Bị Bị Động (Passives)**.
* **Cấp độ:** Nâng cấp từ **Lv 1 đến Lv 5 (MAX)**.
* **Quy tắc Pool:** Đủ 4 trang bị thì chỉ roll ra nâng cấp cho 4 trang bị đó. Khi đạt **Lv 5 (MAX)** $\rightarrow$ **Xóa vĩnh viễn khỏi bể thẻ nâng cấp**.

| Trang Bị Bị Động | Sprite Asset | Lv 1 | Tăng mỗi Cấp (Lv 2 - 4) | Lv 5 (TỐI ĐA - MAX) |
| :--- | :--- | :---: | :---: | :---: |
| **Book of Damage** | `item_book_damage` | +10% Dmg | +10% Dmg / Cấp | **+50% Tổng Sát Thương** |
| **Feather of Speed** | `item_feather_speed` | +6% Move Speed | +6% Tốc độ / Cấp | **+30% Tốc Độ Di Chuyển** |
| **Heart of Life** | `item_heart_max_hp` | +20 Max HP | +20 Max HP / Cấp | **+100 Máu Tối Đa (Hồi ngay)** |
| **Iron Shield** | `item_shield_armor` | +6% Armor | +6% Giáp / Cấp | **+30% Giảm Sát Thương** |
| **Chronos Clock** | `item_clock_cooldown`| -6% Cooldown | -6% Hồi chiêu / Cấp | **-30% Thời Gian Hồi Chiêu** |
| **Ender Eye** | `item_ender_eye_pickup`| +25% Magnet Radius| +25% Tầm hút / Cấp | **+125% Bán Kính Nam Châm** |
| **Potion of Regen** | `item_potion_regen` | +0.5 HP/s | +0.5 HP/s / Cấp | **+2.5 HP/s Hồi Máu Tự Nhiên** |
| **Emerald of Wisdom** | `item_emerald_exp` | +10% EXP Gain | +10% EXP / Cấp | **+50% Lượng EXP Thu Được** |
| **Totem of Undying** | `item_totem_revive` | 1 Hồi Sinh (50% HP)| +10% HP sống lại / Cấp | **1 Hồi Sinh (100% HP + Bất Tử 3s)** |

---

## 6. MA TRẬN TIẾN HÓA VŨ KHÍ THẦN THOẠI (WEAPON EVOLUTION)

> **Điều kiện tiến hóa:** Vũ Khí đạt **Lv 8 (MAX)** + Trang Bị Bị Động ghép cặp tương ứng đạt **Lv 5 (MAX)** $\rightarrow$ Nhặt Rương Lucky Block hoặc nâng cấp sẽ mở khóa Vũ Khí Tiến Hóa Hoàng Kim.

| Vũ Khí Cơ Bản (Lv 8) | Trang Bị Cặp (Lv 5) | Vũ Khí Tiến Hóa Hoàng Kim | Hiệu Ứng Sức Mạnh Thần Thoại |
| :--- | :--- | :--- | :--- |
| **Magma Broadsword** | **Book of Damage** | ☀️ **Solar Flare Greatsword** | Bão lửa 360° bao quanh người chơi, thiêu đốt 40 dmg/s toàn màn hình. |
| **Battle Axe** | **Heart of Life** | 🪓 **Executioner's Greataxe** | Bổ địa chấn hình nón cực đại, sát thương Crit $\times 300\%$, one-hit quái thường. |
| **Twin Daggers** | **Feather of Speed** | 🗡️ **Shadow Phantom Daggers** | Vũ điệu bóng đêm chém liên hoàn 8 nhát kiếm siêu tốc với tốc độ mắt thường khó thấy. |
| **Dark Scythe** | **Iron Shield** | 💀 **Reaper's Harvest** | Hút máu $10\%$ sát thương gây ra thành máu người chơi, tạo khiên chắn linh hồn bảo hộ. |
| **Crystal Laser** | **Chronos Clock** | ⚡ **Oblivion Death Ray** | Tia laze năng lượng kép vĩnh cửu xoay tròn liên tục quét sạch mọi chướng ngại trên đường. |
| **Crossbow** | **Emerald of Wisdom** | 🏹 **Gatling Repeater** | Nỏ thần bắn liên thanh chùm 5 mũi tên ma thuật ngọc bích phát nổ khi găm trúng mục tiêu. |

---

## 7. BẢNG CÂN BẰNG 12 LOÀI QUÁI VẬT (ENEMY ROSTER & SCALING)

*(So sánh với tốc độ Steve = 4.0 m/s)*

| Kẻ Địch | Vai trò Archetype | Máu Đầu (0:00) | Máu Cuối (15:00) | Tốc Độ Gốc | Tăng/Phút | Tốc Cuối (15:00) | Dmg | Bảng Rơi Đồ |
| :--- | :--- | :---: | :---: | :---: | :---: | :---: | :---: | :--- |
| **Enemy_Zombie** | Cận chiến bầy đàn | 50 HP | 238 HP | **3.50 m/s** | +0.03 m/s | 3.95 m/s | 10 | 100% Blue Gem (10 EXP), 5% 💎 |
| **Enemy_Husk** | Cận chiến sa mạc trâu | 75 HP | 356 HP | **3.50 m/s** | +0.03 m/s | 3.95 m/s | 14 | 100% Blue Gem (10 EXP), 6% 💎 |
| **Enemy_ZombieV2** | Zombie giáp sắt | 65 HP | 309 HP | **3.60 m/s** | +0.03 m/s | 4.05 m/s | 12 | 100% Blue Gem (10 EXP), 7% 💎 |
| **Enemy_HuskShovel** | Husk trang bị xẻng | 85 HP | 404 HP | **3.60 m/s** | +0.03 m/s | 4.05 m/s | 16 | 100% Green Gem (25 EXP), 8% 💎 |
| **Enemy_Spider** | Đột kích tốc độ cao | 30 HP | 143 HP | **4.50 m/s** | +0.025 m/s | 4.88 m/s | 8 | 100% Blue Gem (10 EXP), 4% 💎 |
| **Enemy_Vex** | Bay xuyên chướng ngại | 25 HP | 119 HP | **4.80 m/s** | +0.025 m/s | 5.18 m/s | 10 | 100% Blue Gem (10 EXP), 4% 💎 |
| **Enemy_Phantom** | Săn mồi đường không | 35 HP | 166 HP | **4.40 m/s** | +0.025 m/s | 4.78 m/s | 12 | 100% Blue Gem (10 EXP), 5% 💎 |
| **Enemy_Creeper** | Cảm tử nổ chậm | 60 HP | 285 HP | **3.40 m/s** | +0.02 m/s | 3.70 m/s | 45 | 100% Green Gem (25 EXP), 8% 💎 |
| **Enemy_IceCreeper** | Nổ băng làm chậm | 65 HP | 309 HP | **3.40 m/s** | +0.02 m/s | 3.70 m/s | 45 | 100% Green Gem (25 EXP), 8% 💎 |
| **Enemy_Slime** | Nhảy nhót dẻo dai | 60 HP | 285 HP | **3.20 m/s** | +0.02 m/s | 3.50 m/s | 8 | 100% Blue Gem (10 EXP), 5% 💎 |
| **Enemy_Witch** | Ném bình độc tầm xa | 40 HP | 190 HP | **3.30 m/s** | +0.02 m/s | 3.60 m/s | 15 | 100% Green Gem (25 EXP), 10% 💎 |
| **Enemy_Evoker** | Pháp sư triệu hồi | 150 HP | 712 HP | **3.20 m/s** | +0.02 m/s | 3.50 m/s | 25 | 100% Yellow Gem (50 EXP), 15% 💎 |

---

## 8. HỆ THỐNG QUÁI TINH ANH (SINGLE UNIFIED ELITE SYSTEM)

* **Tỉ lệ xuất hiện:** 4% (Normal), 8% (Hard), 15% (Hardcore) từ bất kỳ loài nào trong 12 quái.
* **Nhận diện hình ảnh:**
  - 100% giữ nguyên màu sắc gốc của quái vật (không bị đổi màu, không bị phủ vàng).
  - Vòng hào quang tím thạch anh xoay dưới chân: `vfx_elite_aura_ring` (12 frames, PPU 32, sortingOrder 4). Sprite Pivot đặt tại $\{X: 0.5, Y: 0.2857\}$ (tọa độ pixel 64, 32), neo theo `mobSr.bounds.min.y` $\rightarrow$ Độ lệch với bàn chân = **0.0000**.
  - Thanh máu nhỏ màu tím (`#A855F7`) phía trên đỉnh đầu.
* **Hệ số cường hóa:** Máu $\times 2.5$ (hoặc $\times 3.5$) | Sát thương $\times 1.25$ | Kích thước $\times 1.20$ | Kháng đẩy lùi $+0.25f$.
* **Phần thưởng diệt Tinh Anh:** 100% rơi Rương Lucky Block Vàng (hoặc ngọc vàng 150 EXP) + chùm 3 - 10 Emeralds.

---

## 9. VẬT THỂ TƯƠNG TÁC & KINH TẾ (INTERACTABLES & ECONOMY)

### 9.1. Rương May Mắn Vàng (Breakable Lucky Block Chest):
* **Bản chất:** Là chướng ngại vật tương tác trên sàn (`IDamageable`), **không phải item nhặt**.
* **Độ bền:** 60 HP (cần 3 đòn đánh trúng).
* **Hiệu ứng:** Đánh trúng nảy đàn hồi và hiện chữ: `CRACK! (1/3)` $\rightarrow$ `CRACK! (2/3)`.
* **Phần thưởng bung vỡ:** 70 EXP (1 Vàng 50 EXP + 2 Xanh 10 EXP) + 2 đến 5 Emeralds + ngẫu nhiên:
  - 35% Steak (Hồi 50 HP)
  - 30% Magnet (Hút sạch ngọc toàn map)
  - 25% TNT (Screen Nuke)
  - 10% Jackpot (Bung thêm 15 Emeralds + 150 EXP)

### 9.2. Khối Thuốc Nổ TNT (Instant Screen Nuke):
* Nhặt TNT lập tức hủy diệt **100% quái vật trong khung nhìn camera** (`Camera.main.WorldToViewportPoint`) với **99,999 sát thương**.
* Không có animation nổ chậm chạp; quái ngoài màn hình không bị ảnh hưởng.

### 9.3. Kinh Tế Emerald & Reroll:
* **Tính chất:** Tiền tệ in-run session, reset về 0 khi ván kết thúc (Zero-DB).
* **Reroll Level Up:**
  - Lần Roll đầu tiên của mỗi lần lên cấp: **MIỄN PHÍ (0 💎)**.
  - Các lần Roll tiếp theo: $1\text{ 💎} \rightarrow 2\text{ 💎} \rightarrow \dots$ (tăng $+1\text{ 💎}$, tối đa trần $5\text{ 💎}$).
  - Nhân vật Villager được giảm giá 50% phí Reroll.

---

## 10. TIẾN TRÌNH TRẬN ĐẤU 15 PHÚT (15-MINUTE WAVE TIMELINE)

| Mốc Thời Gian | Giai Đoạn | Quái Vật Xuất Hiện | Mật Độ Quái | Sự Kiện Đặc Biệt |
| :--- | :--- | :--- | :---: | :--- |
| **00:00 - 03:00** | Giai đoạn 1 (Khởi động) | Zombie, Husk, vài Spider lẻ tẻ | 20 - 45 | Người chơi làm quen, nâng cấp 2-3 vũ khí đầu. |
| **03:00 - 06:00** | Giai đoạn 2 (Gia tăng) | ZombieV2, HuskShovel, Creeper | 50 - 90 | **05:00: Nether Golem Mini-Boss (2,000 HP)** rơi Rương Vàng. |
| **06:00 - 10:00** | Giai đoạn 3 (Áp lực cao) | Slime, Ice Creeper, Phantom, bầy Vex | 90 - 140 | Xuất hiện quái bay xuyên địa hình ép di chuyển liên tục. |
| **10:00 - 14:00** | Giai đoạn 4 (Hỗn chiến) | Witch, Evoker, bầy Elite xuất hiện | 140 - 180 | **10:00: Nether Golem Enraged (3,500 HP)** rơi Rương Vàng. |
| **14:00 - 15:00** | Giai đoạn 5 (Cao trào) | Toàn bộ 12 loài quái đổ bộ tốc độ cao | 200 - 250 | Đạt vũ khí Lv 8 + Passives Lv 5 để tiến hóa dọn bầy. |
| **15:00** | Đại Chiến Trùm Cuối | **The Warden (12,000 HP)** | - | Tiêu diệt Trùm Cuối để đạt chiến thắng vẻ vang! |

---

## 11. ĐIỀU CHỈNH 3 CHẾ ĐỘ KHÓ (DIFFICULTY MODES)

| Thông Số Cân Chỉnh | **Normal (Bình Thường)** | **Hard (Thử Thách)** | **Hardcore (Ác Mộng)** |
| :--- | :---: | :---: | :---: |
| **Máu quái thường (HP)** | **x1.0** (119 - 404 HP) | **x1.25** (148 - 505 HP) | **x1.50** (178 - 606 HP) |
| **Máu Boss / Mini-Boss** | **x1.0** (Boss 12,000 HP) | **x1.30** (Boss 15,600 HP) | **x1.65** (Boss 19,800 HP) |
| **Tốc độ di chuyển quái** | **x1.0** (Chuẩn) | **x1.15** (+15% tốc độ) | **x1.30** (+30% rượt đuổi) |
| **Sát thương quái lên Player** | **x1.0** | **x1.25** | **x1.50** |
| **Tỉ lệ xuất hiện Tinh Anh** | 4% | 8% | 15% |
| **Giới hạn số quái trên map** | 150 con | 200 con | 250 con |
| **Hệ số thưởng Emerald cuối ván**| x1.0 | **x1.5** | **x2.2** |

---

## 12. RÀNG BUỘC KỸ THUẬT & QUY TẮC PHÁT TRIỂN DÀNH CHO AGENT (AGENT DIRECTIVES)

1. **Vật lý di chuyển:** Bắt buộc sử dụng `Rigidbody2D.linearVelocity` cho chuyển động Top-Down liên tục; không bao giờ dùng `AddForce` hoặc can thiệp `transform.position` thô để tính toán va chạm. Trọng lực `gravityScale = 0`.
2. **Quy chuẩn Sprite PPU = 32:** Mọi asset hình ảnh đưa vào game phải set PPU là 32, Filter Mode là `Point (no filter)` và Compression là `None`.
3. **Phòng chống Treo TimeScale:** Bắt buộc duy trì `Time.timeScale = 1f;` trong `Awake()` của mọi Manager (`PlayerController`, `WaveSpawner`, `InGamePauseManager`) để tránh bị lưu TimeScale = 0 từ edit-mode.
4. **Văn bản TextMeshPro:** Tuyệt đối không dùng component `UnityEngine.UI.Text` cũ. Mọi chữ hiển thị dùng `TextMeshProUGUI` kết hợp với font `VCR_OSD_MONO SDF.asset`.
5. **Tránh rác Scene:** Mọi script sinh chữ bay (`DamageNumber.cs`) phải có guard `if (!Application.isPlaying) return;` để không làm bẩn file Scene khi chạy công cụ trong Unity Editor.
