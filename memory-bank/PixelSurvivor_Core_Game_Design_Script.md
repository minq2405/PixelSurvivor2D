# PIXEL SURVIVOR — CORE GAME DESIGN SPEC v2.1 (REBALANCED + REVIEW PATCH)
> **Bản tổng hợp & cân bằng lại toàn bộ chỉ số** từ `PixelSurvivor_Core_Game_Design_Script.md` (v1).
> Mục tiêu v2: (1) mỗi chế độ khó **cảm nhận được ngay từ phút đầu**, (2) sức mạnh người chơi và sức mạnh quái **tăng song song** để cuối trận vẫn căng như đầu trận, (3) Hardcore thực sự là Hardcore ở mọi thời điểm.
> Quy ước: tên in-game giữ **100% tiếng Anh**; mọi con số mới đã được kiểm chứng bằng mô hình cân bằng vĩ mô (mục 14). Đây là **điểm xuất phát để playtest**, không phải số cuối cùng — mục 15 liệt kê núm vặn (tuning knobs) và KPI.

> ### 🩹 v2.1 — BẢN VÁ SAU REVIEW HỘI ĐỒNG
> Vá dựa trên 2 báo cáo review của v2.0 và phần **tự tính lại** các con số bị nêu. Nhãn: **[SPEC]** = lỗi/mâu thuẫn/thiếu định nghĩa (bắt buộc sửa trước khi code) · **[CÂN BẰNG]** = số đã chỉnh, vẫn là **giá trị khởi điểm để playtest**. Mục **17** ghi các quyết định tạm cho các câu hỏi mở; mục **18** là việc chưa vá.
>
> | # | Mục | Thay đổi v2.0 → v2.1 |
> | :-: | :-: | :--- |
> | 1 | 2.5 | **[SPEC]** I-frame chạm: chưa rõ phạm vi, 0.5s → **toàn cục 0.35s** + hệ số cộng dồn khi bị vây (+25%/quái chạm thêm, tối đa +100%). Nổ/độc/đạn/chiêu Boss dùng khóa riêng. Regen nội tại cũng ngưng hồi sau khi bị trúng. |
> | 2 | 5, 5.2 | **[SPEC]** Evolution Pity mâu thuẫn với giới hạn 4 passive → viết lại: Pity bắt buộc xuất hiện trong ≤3 lượt, thêm thẻ **Swap**; passive Lv5 **vẫn chiếm slot**. |
> | 3 | 2.6, 13, 14 | **[SPEC]** Mục tiêu level tự mâu thuẫn (Lv46–48 vs 51 lượt = Lv52; tổng EXP thực 67,260 ≠ 65,000). Chốt **45–46 lượt lúc 15:00**; EXP đổi `15×Lv^1.35` → `20×Lv^1.20` (Lv1: 15 → 20 EXP). |
> | 4 | 10.3, PL-A | **[SPEC]** Reroll lần thứ 6 của Villager không có giá (mảng 5 phần tử) → thêm **18 💎**. |
> | 5 | 3, 4.2, 5 | **[SPEC]** Villager tốc độ tối đa 5.6 → **5.2**; Totem 35/45/55/65/80 → **40/50/60/70/80**; Burn Broadsword nằm ngoài ngân sách DPS → gộp vào ngân sách (hạ sát thương chém). |
> | 6 | 7.1, 8, 9, 10 | **[SPEC]** Quái triệu hồi (Vex, Slime con, Sculk Burst, Zombie của Golem) **không rơi EXP/💎**, không tính tiến độ Elite/hồi máu; **cap quái là cap toàn cục** (gồm cả Elite và quái triệu hồi). |
> | 7 | 14 | **[SPEC]** Mục 14 dùng Steve để mô phỏng Build A (Steve không thể có Daggers) → đánh dấu mô hình v2.0 là tham khảo, thêm 3 chỉ số áp lực + bảng độ nhạy. |
> | 8 | 16 | **[SPEC]** Bỏ `Time.timeScale = 1f` trong `Awake()` mọi Manager (có thể bỏ pause) → một `TimeScaleManager` duy nhất; thêm quy tắc hiệu năng. |
> | 9 | 9 | **[CÂN BẰNG]** Warden: Enrage 150/120/100s → **180/150/130s**, Enrage thêm +35% tốc độ; HP mult boss 1.00/1.25/1.55 → **1.00/1.20/1.40**; Charge 70 → **55**; thêm Sculk Tendril Pull chống kite tầm xa. |
> | 10 | 3, 2.4, 4.2 | **[CÂN BẰNG]** Alex+Daggers: trần CDR 40% → **30%**; Daggers Crit +15 → **+8** điểm %. |
> | 11 | 6, 9.1 | **[CÂN BẰNG]** Chặn Evolution Rush: tiến hóa **chỉ từ 9:00**, tối đa **2 vũ khí tiến hóa**/ván; rương Golem I chỉ +1 cấp trang bị, rương Golem II mới đảm bảo tiến hóa. |
> | 12 | 6 | **[CÂN BẰNG]** Đưa tiến hóa về ≈×1.5: Executioner 120 DPS (Execute 10%), Reaper 106 DPS, Solar Flare tổng 131 DPS/mục tiêu; làm lại **Oblivion Death Ray** (tia rộng 0.8m, trần 15 mục tiêu, hãm xoay khi quét Boss). |
> | 13 | 4.2 | **[CÂN BẰNG]** Crossbow: xuyên tối đa 3, −25%/lần xuyên (tổng ×2.25 thay vì ×3.1). Daggers/Crossbow ưu tiên Boss > Elite > gần nhất. |
> | 14 | 11.3 | **[CÂN BẰNG]** Pincer: bán kính 2m → **4.5m**, Hardcore 10 → **8 Zombie**, telegraph 1.5s, chừa góc thoát 60°. |
> | 15 | 8.4 | **[CÂN BẰNG]** Pity rương Elite +25% → **+10%**, trần rương 12 → **10/9/8** theo mode (tỉ lệ hiệu dụng ~42/40/39% → ~34/32/31%). |
> | 16 | 8.3, 7 | **[CÂN BẰNG]** Affix Swift +20% → +12% (không trên Spider/Vex/Phantom, không đi cùng Berserk); Regenerating 2% → 1%; Zombie/Husk/ZombieV2/HuskShovel tăng tốc +0.03 → **+0.02**/phút. |
> | 17 | 3, 10.3 | **[CÂN BẰNG]** Villager Emerald ×2 → **×1.5**. Potion of Regen 0.4→2.0 HP/s, ngưng hồi 1.0s. TNT: bán kính cố định 12m, giới hạn EXP/💎, không hồi máu Scythe. |
> | 18 | 10.1, 5.1 | **[CÂN BẰNG]** Lucky Block EXP bỏ sàn 70 (tối thiểu 5); Bounty Mark ≤3 lần/ván; Scholar's Tome không cộng dồn. |
> | 19 | 15 | KPI mới (Gross Damage Taken, P90 TTK < Enrage, % chạm Enrage, thời điểm tiến hóa, hiệu năng) + telemetry bổ sung. |
>
> **Giữ nguyên có chủ đích:** công thức HP quái, sát thương quái theo thời gian, bảng mật độ, hệ số EXP mode (1.00/1.10/1.25), số đòn để chết — đều đã được tái tính khớp.

---

## 0. CHẨN ĐOÁN v1 — VÌ SAO GAME BỊ QUÁ DỄ VỀ CUỐI TRẬN

Mô hình vĩ mô (mục 14) chạy trên đúng số liệu v1 cho thấy:

| # | Vấn đề | Bằng chứng (v1) | Hệ quả |
| :-: | :--- | :--- | :--- |
| 1 | **Vũ khí scale quá mạnh** | DPS mỗi vũ khí Lv1→Lv8 tăng **×8.1 – ×24.9** (Broadsword ×8.1, Daggers ×21.5, Crossbow ×24.9). Có 4 slot, nên tổng tăng gấp bội. | Full build cuối trận gây **~750 – 1,990 DPS đơn mục tiêu thô** (chưa tính passive, Crit, tiến hóa). |
| 2 | **Hai vũ khí "phá game"** | Twin Daggers Lv8 = **928 DPS**, Crossbow Lv8 = **630 DPS** (xuyên 5 mục tiêu ⇒ tới ~3,150). Vũ khí còn lại chỉ 130 – 232. | Build có Daggers/Crossbow mạnh gấp **2.7×** build khác; chọn sai là nhàm, chọn đúng là thắng. |
| 3 | **Tổng nhân sức mạnh chồng chất** | Book +50% × Clock −30% CD (×1.43) = **×2.14**; Alex thêm −15% CD ⇒ Daggers CD 0.28s → **0.154s**. Crit Daggers +30%. Burn Broadsword 15 dmg/s ×3s = 45 dmg/đòn > sát thương đòn đánh Lv1 (25). | Không có trần ⇒ sức mạnh mũ hóa. |
| 4 | **Tiến hóa quá khỏe** | Solar Flare: 40 dmg/s **toàn màn hình** ⇒ với 150–250 quái = ~6,000 – 10,000 "DPS tương đương". Executioner: one-hit quái thường. | Phút 13–15 dọn sạch bản đồ, mất hoàn toàn áp lực. |
| 5 | **Quái scale tuyến tính nhẹ, sát thương quái đứng yên** | HP ×4.75 sau 15 phút (tuyến tính), Dmg quái **không đổi theo thời gian**. Trong khi Heart (+100 HP) + Shield (+30%) + Regen (+2.5/s) làm "số đòn để chết" tăng gần ×2.7 (Normal 13.3 → 36.7 đòn). | Cuối trận hầu như không thể chết. |
| 6 | **Boss/Mini-boss làm bằng giấy** | Thời gian hạ (ước tính, uptime 70%): Golem I **10 – 16s**, Golem II **4 – 9s**, **The Warden chỉ 1.8 – 6.5s** (12,000 HP). | Trận chung kết kết thúc trước khi kịp nhìn thấy chiêu thức. |
| 7 | **Hardcore dễ hơn Normal khi so sánh đầu–cuối** | Chỉ số Áp Lực (Pressure Index, mục 14): Normal đầu trận **~17–25** → cuối **3.8**; Hardcore đầu **~38–56** → cuối **14.4**. ⇒ **Hardcore cuối trận (14.4) dễ hơn Normal ở phút 3 (17.0).** | Đúng nhận xét của bạn: enemy scale quá yếu, nhất là Hardcore. |
| 8 | **Đầu trận 3 mode gần như giống nhau** | Mật độ quái theo timeline là **một bảng chung**; cap quái (150/200/250) chỉ có tác dụng sau phút ~10. Khác biệt đầu trận chỉ là HP ×1/1.25/1.5 trên quái 50 HP. | Không "cảm" được mode khó ở 60 giây đầu. |
| 9 | **Tốc độ quái vượt tốc người chơi ngay phút 0** | Hard: Zombie 3.5×1.15 = **4.03 m/s > Steve 4.0**. Hardcore: Zombie **4.55**, Spider **5.85 > Alex 5.0**. | Kiting (cốt lõi của thể loại) sụp đổ ở Hardcore, người chơi bị đè từ giây đầu — khó **giả tạo**, không phải khó **thú vị**. |
| 10 | **Kinh tế & rương lạm phát** | Elite 15% (Hardcore) + **100% rơi Lucky Block** + 25% TNT (nuke 100% màn hình) + Villager Emerald ×3 + reroll trần 5💎. | Hardcore có nhiều TNT/Magnet/Steak nhất, reroll vô hạn ⇒ càng khó càng được thưởng quá tay. |

**Lỗi/ mâu thuẫn tài liệu v1 cần sửa** (đã sửa trong v2):
- Steve khởi đầu "Flaming Broadsword" ↔ bảng vũ khí "Magma Broadsword" → thống nhất **Magma Broadsword**.
- Công thức 2.1 nói HP quái thường "150–450" nhưng bảng thực tế **119 – 712** (Evoker).
- Timeline mật độ 200–250 quái nhưng cap Normal/Hard chỉ 150/200.
- Elite "Máu ×2.5 (hoặc ×3.5)" mơ hồ.
- Totem: +10%/cấp từ 50% chỉ tới 80% ở Lv4 rồi nhảy 100% ở Lv5.
- Chưa định nghĩa **tick sát thương chạm** (i-frame), hành vi/chiêu thức Boss, enrage, thẻ level-up khi hết bể thẻ, mục tiêu EXP/level.
- EXP Lv5 = 132 (không phải 133).

---

## 1. TỔNG QUAN DỰ ÁN & VÒNG LẶP CỐT LÕI

* **Tên dự án:** Pixel Survivor
* **Thể loại:** 2D Top-Down Roguelite Action Survivor (loop Vampire Survivors + phong cách Minecraft).
* **Nền tảng & Engine:** PC & Mobile | Unity 6 LTS (6000.4.10f1) | URP 2D.
* **Chuẩn Pixel Art:** PPU = **32** cho mọi sprite.
* **Ngôn ngữ In-game:** 100% Tiếng Anh.
* **Kiến trúc Zero-DB:** mọi tài nguyên (EXP, Emeralds, nâng cấp) chỉ sống trong một lượt chơi.
* **Độ dài trận:** 15:00 + trận Boss cuối (The Warden xuất hiện lúc 15:00).

### Vòng lặp 15 phút
```
[Di chuyển & Né tránh] ➔ [Tự động tấn công] ➔ [Nhặt EXP & Emeralds]
       ▲                                              │
       │                                              ▼
[Tiến hóa Vũ khí / Diệt Boss] ◄── [Chọn Thẻ Nâng Cấp (4 Vũ khí + 4 Passives)]
```

### Đường cong cảm giác mục tiêu (Design Curve)
| Giai đoạn | Cảm giác Normal | Cảm giác Hard | Cảm giác Hardcore |
| :--- | :--- | :--- | :--- |
| **0:00–1:00 (Mở đầu)** | Thong thả, vừa đi vừa đánh | Phải chủ động kiting, đứng yên là mất máu | Bị vây từ giây 20; mỗi sai lầm đều trả giá; có Elite đầu tiên lúc 1:00 |
| **1:00–5:00** | Học build | Phải ưu tiên thẻ sống sót | Mỗi thẻ là quyết định sinh tử; sự kiện **Pincer** lúc 1:30 |
| **5:00–10:00 (Giữa)** | Cảm giác "mạnh lên" rõ | Áp lực đều, cần di chuyển liên tục | Quái dày, Elite liên tục, boss mini cần cơ chế |
| **10:00–15:00 (Cuối)** | **Vẫn căng** (Pressure ≈ đầu trận) | Vẫn căng, cần build hoàn chỉnh | Vẫn nghẹt thở, cần build tối ưu + kỹ năng |
| **Boss cuối** | ~55–75s, phải né chiêu | ~67–91s | ~77–104s, có enrage (build đủ, uptime 70%) |

---

## 2. CÔNG THỨC TOÁN HỌC CỐT LÕI (v2)

### 2.1. Máu quái — thang HP theo thời gian & chế độ (có "Final Surge")
$$\text{HP}(t,m) = \text{HP}_{\text{base}} \times H0_m \times (1 + s_m \cdot t) \times \text{Surge}(t)$$
$$\text{Surge}(t) = 1 + 0.35 \times \text{clamp}\!\left(\frac{t-12}{3},\,0,\,1\right)$$
*(t = phút trong trận [0, 15])*

| Chế độ | H0 (HP gốc) | s (độ dốc/phút) |
| :--- | :---: | :---: |
| Normal | 1.00 | 0.24 |
| Hard | 1.10 | 0.26 |
| Hardcore | 1.22 | 0.29 |

**Hệ số HP thực tế (HPScale):**

| Phút | 0 | 1 | 3 | 5 | 8 | 10 | 12 | 13.5 | 15 |
| :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Normal** | 1.00 | 1.24 | 1.72 | 2.20 | 2.92 | 3.40 | 3.88 | 4.98 | **6.21** |
| **Hard** | 1.10 | 1.39 | 1.96 | 2.53 | 3.39 | 3.96 | 4.53 | 5.83 | **7.28** |
| **Hardcore** | 1.22 | 1.57 | 2.28 | 2.99 | 4.05 | 4.76 | 5.47 | 7.05 | **8.81** |

> v1: ×4.75 ở mọi mode. v2: ×6.2 / ×7.3 / ×8.8. Đoạn **Final Surge (12:00–15:00)** là "đối trọng" cho vũ khí tiến hóa, giữ áp lực không rơi ở những phút cuối.

### 2.2. Tốc độ quái
$$\text{Speed}(t,m) = (\text{Speed}_{\text{base}} + k \cdot t) \times \text{SpeedMult}_m$$
| Chế độ | SpeedMult |
| :--- | :---: |
| Normal / Hard / Hardcore | **1.00 / 1.05 / 1.10** (v1: 1.00 / 1.15 / 1.30) |

**Quy tắc thiết kế tốc độ (bắt buộc):**
1. **Quái "chaser" thường** (Zombie, Husk, Slime...) luôn ≤ **0.95 × tốc độ Steve gốc (4.0)** ở phút 0, và ≤ **1.0×** ở phút 15 — người chơi luôn có thể kiting nếu không bị vây.
2. **Quái nhanh** (Spider, Vex, Phantom) được phép nhanh hơn người chơi **nhưng máu ≤ 0.6× máu trung bình** ⇒ giải quyết bằng sát thương, không phải bằng chạy.
3. **Tốc độ người chơi** (kể cả Feather) bị chặn **+40% tổng** (xem 5).
4. **(v2.1)** Chaser chậm (Zombie, Husk, ZombieV2, HuskShovel) có độ tăng tốc **k = +0.02 m/s/phút** (v2.0: +0.03) để Hardcore phút 15 vẫn còn biên kiting ≥ 6% (ZombieV2: 3.74 so với Steve 4.0). Affix `Swift` không random lên Spider/Vex/Phantom và không đi cùng `Berserk` (xem 8.3).

### 2.3. Sát thương người chơi & Chí mạng
$$\text{HitDMG} = \text{DMG}_{\text{weapon}} \times (1 + \text{BonusDmgPct})$$
$$\text{FinalDMG} = \text{HitDMG} \times \text{CritMult} \text{ nếu Random} < \text{CritChance, ngược lại } \text{HitDMG}$$
* `BonusDmgPct` cộng dồn (Book of Damage + Iron Will); **không** có nhân chồng nhân. **(v2.1) Tổng trần +60%** (hiện Book 40% + Iron Will 20% vừa chạm trần; chặn các nguồn thêm sau này).
* `CritChance` trần **60%**. Mọi ghi chú dạng "Crit +X%" của vũ khí nghĩa là **+X điểm phần trăm Crit Chance**, không phải Crit Damage.

### 2.4. Hồi chiêu — trần chung (MỚI)
$$\text{CD}_{\text{final}} = \text{CD}_{\text{base}} \times \max\!\left(0.70,\; 1 - \sum \text{CDR}\right)$$
* **(v2.1) Trần CDR −30%** (v2.0: −40%). Chronos Clock (−25%) + Agility của Alex (−10%) = −35% ⇒ **bị cắt còn −30%**, ngăn Alex+Daggers chạm hệ số ×2.15.
* Sàn tuyệt đối: Twin Daggers không bao giờ dưới **0.25s**.

### 2.5. Giáp & sát thương nhận
$$\text{DamageTaken} = \max\!\left(1,\; \text{DMG}_{\text{enemy}}(t) \times (1 - \min(\text{Armor}, 0.35))\right)$$
* Armor tổng trần **35%** (v1: ~40% và không có trần).
* **Sát thương quái tăng theo thời gian (MỚI):**
$$\text{DMG}_{\text{contact}}(t,m) = \text{DMG}_{\text{base}} \times (1 + 0.05\,t) \times \text{DamMult}_m$$
  Sát thương nổ (Creeper/Ice Creeper) dùng $(1 + 0.025\,t)$ — tăng chậm hơn một nửa để tránh one-shot.
* **I-frame chạm (v2.1 — đã chốt phạm vi): TOÀN CỤC 0.35s.** Sau mỗi đòn chạm, mọi sát thương *chạm* khác bị chặn 0.35s (không tính riêng từng quái). v2.0 ghi 0.5s nhưng không nêu phạm vi: nếu toàn cục thì mật độ quái không làm tăng sát thương nhận, nếu theo từng quái thì Pincer 10 Zombie gây ~260 HP/s.
* **Cộng dồn khi bị vây (MỚI)** — để mật độ quái vẫn tạo áp lực dù I-frame toàn cục:
$$\text{DMG}_{\text{hit}} = \text{DMG}_{\text{contact}}(t,m) \times \big(1 + 0.25 \times \min(N_{\text{extra}},\,4)\big)$$
  với $N_{\text{extra}}$ = số quái *khác* đang chạm người chơi tại lúc trúng đòn (tối đa +100%).
* **Nguồn KHÔNG phải chạm** (nổ Creeper/IceCreeper/Volatile, vũng độc Witch, đạn, chiêu Boss) **không** dùng I-frame chạm; mỗi *thực thể gây sát thương* có khóa riêng 0.25s lên người chơi (một vụ nổ trúng 1 lần, vũng độc tick 0.5s).
* **Regen (v2.1):** Regen nội tại của nhân vật *và* Potion of Regen đều **ngưng 1.0s sau khi bị trúng đòn** (v2.0 chỉ Potion, 2s). Lý do: Regen 0.5 HP/s của Steve = 30 HP/phút ≈ 25% Max HP/phút, làm méo KPI "% HP mất/phút".

### 2.6. Đường cong EXP (v2.1 — đổi công thức & chốt mục tiêu)
$$\text{EXP}_{\text{required}}(Lv) = \text{Round}(20 \times Lv^{1.20})$$
*(Lv1: 20 | Lv2: 46 | Lv5: 138 | Lv10: 317 | Lv20: 728 | Lv30: 1,185 | Lv45: 1,927)* — v2.0: `15×Lv^1.35` (Lv1: 15, Lv10: 336, Lv45: ~2,500).

* **Lý do đổi:** (1) v2.0 lên cấp ở giây 6–8 (Lv1 = 15 EXP = 2 Zombie) khiến bảng thẻ bật liên tục 5–6 lần trong phút đầu; (2) mô phỏng của review cho thấy với đường cong cũ người chơi thực tế chỉ đạt khoảng 38–41 lượt lúc 15:00; (3) đường cong phẳng hơn giữ nhịp lên cấp đều. Đường cong mới nhỉnh hơn ở các cấp đầu (Lv5: 247 vs 216 EXP tích lũy) và rẻ hơn ~20% ở cuối.
* **Mục tiêu tiến trình (đã chốt): 45–46 lượt chọn lúc 15:00 (Lv46–47) ở Normal.** Không phải 51: v2.0 mâu thuẫn (mục 2.6/13 ghi Lv46–48, mục 14 giả định 51 lượt = Lv52). Đủ 51 lượt (3 mở vũ khí + 28 nâng vũ khí + 20 nâng passive) là *phần thưởng cho người chơi giỏi*, không phải mặc định.
* **EXP tích lũy cần (công thức v2.1):** 45 lượt = **40,384** · 46 lượt = **42,363** · 51 lượt = **53,035**. (v2.0: 50,273 / 52,908 / **67,260** — con số "≈65,000" ghi ở v2.0 là sai.)
* **UI onboarding:** ở level-up đầu tiên chỉ hiển thị thẻ vũ khí/passive ở dạng rút gọn (không văn bản cơ chế dài).

**Hệ số EXP bù cho mode khó (MỚI):** gem EXP ×**1.00 / 1.10 / 1.25** (quái trâu hơn ⇒ hạ chậm hơn ⇒ nếu không bù, Hardcore lên cấp chậm hơn ~20% và càng yếu hơn, kéo thành "khó vì thiếu build").

### 2.7. Chỉ số Áp Lực (Pressure Index — công cụ cân bằng)
$$\text{PI}(t,m) = \frac{\text{Density}(t,m) \times \overline{\text{HP}}(t,m) \times (\text{Speed}/4.0)}{\text{EffDPS}_{\text{player}}(t)}$$
* PI càng cao = người chơi càng bị đè. Mục tiêu v2: **PI không sụp đổ theo thời gian** (phẳng ở Normal, leo thang nhẹ ở Hard/Hardcore), và tỉ lệ **Normal : Hard : Hardcore ≈ 1 : 1.3 : 1.8 (đầu) → 1 : 1.4 : 2.1 (cuối)**.

> **(v2.1) Giới hạn của PI:** PI không chứa sát thương quái, Armor, MaxHP, Regen, tình trạng bị vây, và `EffDPS_player` chưa có công thức truy xuất được. **Không dùng PI một mình để kết luận khó/dễ.** Dùng thêm 3 chỉ số, log trong telemetry:
> * **Clear Pressure** = tổng HP quái đang sống ÷ DPS thực tế của người chơi.
> * **Survival Pressure** = sát thương dự kiến nhận trong 10 giây ÷ Max HP.
> * **Boss Pressure** = TTK Boss ÷ Enrage timer (mục tiêu P90 < 1.0, tốt nhất ≤ 0.8).

### 2.8. Mật độ quái theo thời gian & chế độ
$$\text{Density}(t,m) = \min\!\big(D_{\text{base}}(t) \times DM_m,\; \text{Cap}_m\big)$$
`D_base` nội suy tuyến tính: **0:00=18 → 3:00=38 → 6:00=72 → 10:00=110 → 14:00=140 → 15:00=150**.

| | Normal | Hard | Hardcore |
| :--- | :-: | :-: | :-: |
| DM (hệ số mật độ) | 1.00 | 1.12 | 1.25 |
| Cap quái | 150 | 180 | 220 |
| 0:00 / 3:00 / 6:00 | 18 / 38 / 72 | 20 / 43 / 81 | 23 / 48 / 90 |
| 10:00 / 14:00 / 15:00 | 110 / 140 / 150 | 123 / 157 / 168 | 138 / 175 / 188 |

*(Cap cao hơn mật độ theo công thức một chút để dành chỗ cho sự kiện Swarm/Pincer/Siege ở mục 11.)*

**(v2.1) Định nghĩa:** `Density` = **số quái sống đồng thời** mục tiêu trên bản đồ; `Cap` là **cap toàn cục** gồm quái thường + Elite + quái triệu hồi + Slime con. Quái triệu hồi/Slime con được giới hạn thêm **sub-cap 25 con**. Cơ chế bù quái xem mục 17 (#1). Lưu ý: ở Normal 15:00, Density = Cap = 150 — an toàn vì Warden xuất hiện lúc 15:00 và giảm spawn 60%; trước đó mật độ thấp hơn cap (ví dụ 12:30 ≈ 129).

---

## 3. THÔNG SỐ 3 NHÂN VẬT KHỞI ĐẦU

| Chỉ số | **Steve (The Iron Survivor)** | **Alex (The Swift Ranger)** | **Villager (The Fortune Merchant)** |
| :--- | :---: | :---: | :---: |
| **Lối chơi** | Đấu sĩ cận chiến / Tank | Xạ thủ / Cơ động | Kinh tế / Reroll / Hút ngọc |
| **Max HP** | 120 | 85 | 100 |
| **Regen** | 0.5 HP/s | 0.0 HP/s | 0.2 HP/s |
| **Tốc độ chạy** | 4.0 m/s (chuẩn) | **4.8 m/s (+20%)** *(v1: 5.0)* | **4.2 m/s (+5%)** *(v1: 4.4)* |
| **Armor** | 10% | 0% | 0% |
| **Crit Chance** | 5% | **12%** *(v1: 15%)* | 8% |
| **Crit Mult** | 180% | **190%** *(v1: 200%)* | 170% |
| **Magnet** | 1.7 m | 1.7 m | **2.2 m (+30%)** *(v1: 2.38)* |
| **Vũ khí khởi đầu** | Magma Broadsword | Twin Daggers | Crossbow |
| **Nội tại** | *Iron Will:* +20% Dmg khi < 50% HP | *Agility:* **−10% Cooldown** *(v1: −15%)* | *Merchant Luck:* Reroll −50% phí, Emerald rơi **×1.5** *(v2.0: ×2; v1: ×3)*, +1 Reroll tối đa mỗi lần lên cấp |

**Lý do chỉnh:** Alex là build mạnh nhất ở v1 (Daggers + Agility + Crit cao + tốc độ 5.0 ⇒ phối với Feather +30% = 6.5 m/s, nhanh hơn mọi quái). Giảm nhẹ để Alex vẫn "glass cannon" nhưng không thoát khỏi cả hệ thống kiting. Villager: Emerald ×3 kết hợp reroll giá rẻ tạo vòng lặp tiền không có đáy.

**Tốc độ di chuyển tổng** = $4.0 \times (1 + \text{bonus nhân vật} + \text{bonus Feather})$, **trần +40%** (Steve tối đa 5.0 · Alex tối đa 5.6 *(5.8 bị cắt)* · Villager tối đa **5.2** *(= 4.0×(1+0.05+0.25); v2.0 ghi 5.6 là tàn dư v1)*).

**Regen nội tại (v2.1):** Steve 0.5 / Villager 0.2 HP/s **ngưng 1.0s sau khi bị trúng đòn** (xem 2.5).

---

## 4. HỆ THỐNG VŨ KHÍ TẤN CÔNG (6 WEAPONS, Lv1 → Lv8)

* **Slot:** tối đa **4 vũ khí**. Tự động bắn/chém khi có mục tiêu `IDamageable` trong tầm; sprite luôn quay phải (0°).
* **Triết lý v2:** mỗi vũ khí có **ngân sách DPS theo cấp** gần bằng nhau; vai trò được phân biệt bằng **số mục tiêu trúng** (AoE vs đơn mục tiêu) và tiện ích. Tổng tăng Lv1→Lv8 = **×4 – ×5** (v1: ×8.1–×24.9).

### 4.1. Ngân sách DPS theo cấp (B)
$$B(L)=\begin{cases}22 \times 2^{(L-1)/3} & L \le 4\\ 44 \times 2^{(L-4)/4} & L > 4\end{cases}$$

| Lv | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
| :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **B (DPS/mục tiêu)** | 22.0 | 27.7 | 34.9 | 44.0 | 52.3 | 62.2 | 74.0 | **88.0** |

DPS mỗi vũ khí = $B(L) \times \text{Role}$ (Daggers Lv1 được giảm riêng ×1.56 thay vì ×2.0 để Alex không vượt trội phút đầu).

| Vũ khí | Role | Mục tiêu trung bình (swarm) |
| :--- | :-: | :-: |
| Magma Broadsword | ×1.00 | 3 |
| Battle Axe | ×0.90 | 3.5 |
| Twin Daggers | ×2.00 *(Lv1: ×1.56)* | **1** (đơn mục tiêu, chuyên Elite/Boss) |
| Dark Scythe | ×0.80 | 4 |
| Crystal Laser | ×0.95 | 3 |
| Crossbow | ×1.00 | 3 |

### 4.2. Bảng vũ khí v2

| Vũ khí | Loại / Tầm | Lv 1 | Lv 4 | Lv 8 | DPS (L1/L4/L8) | Cơ chế đặc biệt (v2) |
| :--- | :--- | :---: | :---: | :---: | :---: | :--- |
| **Magma Broadsword** | Chém vòng cung 160° (2.6m → Lv8: 3.0m) | 20 · CD 1.10 | 36 · CD 0.95 | **62** · CD 0.80 | 22.2 / 44.9 / **87.5** *(gồm Burn)* | Knockback 4.0. **Burn: 4 (Lv1) → 7 (Lv4) → 10 dmg/s (Lv8), 3s, không cộng dồn**, **tính vào ngân sách DPS** *(v2.0: sát thương chém 24/42/70 + Burn ngoài ngân sách ⇒ thực tế 25.8/50.6/97.5; v1: 15 dmg/s)*. |
| **Battle Axe** | Bổ chấn động (2.8m → 3.2m) | 34 · CD 1.70 | 60 · CD 1.50 | **100** · CD 1.25 | 20.0 / 40.0 / **80.0** | Knockback 5.5, **Stagger 0.4s**. Bỏ dòng "Crit 450–650" (dùng công thức Crit chuẩn). |
| **Twin Daggers** | Đâm liên hoàn mục tiêu gần nhất (1.8m) | 12×2 · CD 0.70 | 16×3 · CD 0.55 | **18×4** · CD 0.42 | 34.3 / 87.3 / **171.4** | Chém luân phiên, **Crit +8 điểm %** Crit Chance *(v2.0: +15; v1: +30)*. Sàn CD 0.25s. Mạnh nhất vs Elite/Boss. |
| **Dark Scythe** | Quét 270° quanh thân (3.0m) | 24 · CD 1.35 | 41 · CD 1.15 | **67** · CD 0.95 | 17.8 / 35.7 / **70.5** | Hồi **1 HP/kill** (Lv5+: 1.5), **trần 5 HP/s** *(v1: 3 HP/kill không trần)*. |
| **Crystal Laser** | Tia thẳng xuyên thấu (7.0m) | 37 · CD 1.75 | 61 · CD 1.45 | **92** · CD 1.10 | 21.1 / 42.1 / **83.6** | Xuyên toàn bộ mục tiêu trên đường, không góc chết. |
| **Crossbow** | Bắn chùm (8.0m) | 21 · CD 0.95 | 2×18 · CD 0.80 | **3×17.5** · CD 0.60 | 22.1 / 45.0 / **87.5** | **Xuyên 2 → 3 → 3 mục tiêu** (Lv1/4/8), **−25% dmg gốc mỗi lần xuyên**. Lv8 thẳng hàng 3 mục tiêu: 87.5×(1+0.75+0.50) = **~197 DPS tổng** *(v2.0: xuyên 4, −15% ⇒ ~271; v1: xuyên 5, không giảm dmg)* |

### 4.3. So sánh DPS đơn mục tiêu Lv8 (v1 → v2)
| Vũ khí | v1 Lv8 | v2 Lv8 | Thay đổi |
| :--- | :-: | :-: | :-: |
| Magma Broadsword | 184.6 | 87.5 | −53% |
| Battle Axe | 231.6 | 80.0 | −65% |
| **Twin Daggers** | **928.6** | **171.4** | **−82%** |
| Dark Scythe | 200.0 | 70.5 | −65% |
| Crystal Laser | 129.4 | 83.6 | −35% |
| **Crossbow** | **630.0** | **87.5** | **−86%** |
| **Full build 4 vũ khí Lv8 (thô)** | 746 – 1,990 | **322 – 409** | Chênh lệch giữa build mạnh/yếu: **2.7× → 1.27×** |

> Vũ khí bị giảm nhiều nhất là Crossbow/Daggers vì chúng là nguyên nhân chính của phá game. Phần "mất" được bù bằng quái bớt **trâu quá mức ở giai đoạn đầu** và cơ chế tiến hóa (mục 6) rõ ràng hơn.

### 4.4. Quy tắc bổ sung (v2.1)
* **Hai loại DPS cần log riêng:** `SingleTargetDPS` (bảng 4.2/4.3) và `TotalDPS` (cộng mọi mục tiêu trúng). Telemetry phải ghi `TargetsHitPerAttack[weaponId]` (xem 15.3).
* **Ưu tiên mục tiêu của Twin Daggers & Crossbow:** `Boss > Elite > mục tiêu gần nhất` trong tầm. Tránh Daggers đâm quái con khi Warden triệu hồi Sculk Burst (làm uptime lên Boss tụt).
* **Alex + Twin Daggers (kiểm tra lại sau khi vá):** với Book Lv5, Clock Lv5 (CDR cắt −30%), Crit 12%+8% = 20%×190% ⇒ ~**405 DPS** đơn mục tiêu (v2.0: ~459). Steve + Broadsword cùng passive ≈ **163 DPS** (đòn đánh 3 mục tiêu ⇒ tổng ~490). Tức Alex vượt Steve ~2.4× **chỉ khi một mục tiêu** (Elite/Boss) — chấp nhận làm vai trò "glass cannon diệt Boss" (85 HP, 0% Armor), nhưng **theo dõi tỉ lệ thắng Alex ≤ 35% tổng** (mục 15.2).
* Con số "chênh lệch build mạnh/yếu 1.27×" ở 4.3 chỉ đúng với **tổng DPS thô Lv8, chưa tính nhân vật & passive**; không dùng để kết luận cân bằng nhân vật.

---

## 5. HỆ THỐNG TRANG BỊ BỊ ĐỘNG (9 PASSIVES, Lv1 → Lv5)

* **Slot:** tối đa **4 passive**. Đủ 4 ⇒ chỉ roll nâng cấp cho 4 món đó (ngoại lệ duy nhất: thẻ **Swap** của Evolution Pity, mục 5.2). Đạt Lv5 ⇒ **xóa khỏi bể thẻ nhưng VẪN CHIẾM SLOT** *(v2.1 chốt rõ)*.
* **Trần chung (Hard Caps):** Armor tổng ≤ 35% · **CDR tổng ≤ 30%** *(v2.0: 40%)* · Move Speed tổng ≤ +40% · Crit Chance ≤ 60% · **Bonus Damage tổng ≤ +60%** *(MỚI)*.

| Passive | Lv1 | Mỗi cấp | **Lv5 (MAX)** v2 | v1 Lv5 | Ghi chú |
| :--- | :-: | :-: | :--- | :--- | :--- |
| **Book of Damage** | +8% Dmg | +8% | **+40% Sát thương** | +50% | Cặp: Magma Broadsword |
| **Feather of Speed** | +5% Move | +5% | **+25% Tốc độ** | +30% | Cặp: Twin Daggers; trần +40% tổng |
| **Heart of Life** | +15 Max HP | +15 | **+75 Max HP** (hồi lượng tăng thêm) | +100 | Cặp: Battle Axe |
| **Iron Shield** | +5% Armor | +5% | **+25% Armor** | +30% | Cặp: Dark Scythe; trần 35% tổng |
| **Chronos Clock** | −5% CD | −5% | **−25% Cooldown** | −30% | Cặp: Crystal Laser; trần **−30%** tổng (gồm cả Agility của Alex) |
| **Ender Eye** | +20% Magnet | +20% | **+100% Bán kính Magnet** (trần 4.5m) | +125% | Không cặp tiến hóa |
| **Potion of Regen** | +0.4 HP/s | +0.4 | **+2.0 HP/s** | +2.5 | Không cặp tiến hóa; ngưng hồi **1.0s** sau khi bị đánh *(v2.0: 0.3→1.5 HP/s, ngưng 2s — thẻ "bẫy" vì uptime thực tế < 40%)* |
| **Emerald of Wisdom** | +8% EXP | +8% | **+40% EXP** | +50% | Cặp: Crossbow. **(v2.1)** Lv5: thêm **+10% sát thương lên Elite/Boss** (tránh thành slot chết ở trận Warden) |
| **Totem of Undying** | 1 Hồi sinh · **40%** HP | +10% / cấp | **1 Hồi sinh · 80% HP + Bất tử 2s** *(v1: 100% + 3s)* | 100% + 3s | Lv1–5: **40/50/60/70/80%** *(v2.0: 35/45/55/65/80 — nhảy +15% ở Lv5, lặp lỗi v1)* |

**Hiệu ứng tổng:** nhân hệ số sát thương × hồi chiêu của passive đầy đủ giảm từ **×2.14 → ×1.87** (1.40 / 0.75) cho Steve/Villager; **Alex** có thêm Agility nên là 1.40 / 0.70 = **×2.00** sau khi cắt trần CDR (v2.0 không cắt: ×2.15).
**Sống sót:** "số đòn để chết" (Zombie, Steve) giữ ở mức hợp lý: Normal 13.3 → **17.1** (v1: 36.7), Hardcore 10.3 → **13.2** (v1: 24.4).

### 5.1. Bể thẻ khi hết nâng cấp (MỚI)
Khi vũ khí & passive đều đã kịch trần, thẻ level-up nhận từ bể dự phòng (không bao giờ có lượt lên cấp "trống"):
* **Hearty Meal** — hồi 30% Max HP.
* **Emerald Pouch** — +10 💎 (Villager +20).
* **Scholar's Tome** — +25% EXP cho 60s, **không cộng dồn** (nhận lại = làm mới thời lượng).
* **Bounty Mark** — Elite kế tiếp chắc chắn rơi Lucky Block; **tối đa 3 lần/ván**, vẫn tuân theo trần rương Elite và khoảng cách rương ≥ 20s.

### 5.2. Bảo đảm tiến hóa (Evolution Pity — v2.1 viết lại)
v2.0 mâu thuẫn: đủ 4 passive thì không được roll passive mới, nhưng Pity lại tăng trọng số passive đó. Quy tắc mới:
* **Chỉ bật từ 8:00** (tiến hóa mở khóa từ 9:00, xem mục 6).
* Vũ khí **Lv8** + passive cặp **chưa sở hữu** + còn slot ⇒ trọng số **×3** và **bắt buộc xuất hiện trong tối đa 3 lượt level-up kế tiếp** (lượt thứ 3 là 100%).
* Vũ khí Lv8 + passive cặp **đã sở hữu nhưng chưa Lv5** ⇒ trọng số **×2**.
* Vũ khí Lv8 + **đủ 4 passive mà không có passive cặp** ⇒ xuất hiện thẻ **Swap**: đổi 1 passive đang sở hữu lấy passive cặp (bắt đầu **Lv1**, mất cấp cũ). Tối đa 1 thẻ Swap/vũ khí; người chơi được bỏ qua.
* **Không bao giờ** sở hữu quá 4 passive.
* **UI:** thẻ vũ khí hiển thị `Evolution: <passive cặp>`; thẻ passive hiển thị `🔗 Evolves: <vũ khí>` (sáng khi đang cầm vũ khí đó) hoặc `Utility — No Evolution` (Ender Eye, Potion of Regen, Totem). Chỉ thông tin, không cảnh báo kiểu trừng phạt.

---

## 6. MA TRẬN TIẾN HÓA VŨ KHÍ THẦN THOẠI

> **Điều kiện:** vũ khí **Lv8** + passive cặp **Lv5**. Kích hoạt khi nhặt Lucky Block / ở lần lên cấp kế tiếp (thẻ EVOLVE) / **rương Golem II (10:00)** (luôn đảm bảo nếu đủ điều kiện).
> **Khóa v2.1 (chặn "Evolution Rush" lúc 5:00):** **không thể tiến hóa trước 9:00**; **tối đa 2 vũ khí tiến hóa mỗi ván**; rương Golem I (5:00) chỉ +1 cấp cho trang bị (không tiến hóa). Lý do: 12 lượt (vũ khí khởi đầu Lv8 + passive Lv5) < 14 lượt có được lúc 5:00 — min-maxer có thể lấy Solar Flare ~7.5 phút sớm hơn dự kiến.
> **Nguyên tắc v2:** tiến hóa ≈ **×1.5 DPS đơn mục tiêu** của vũ khí gốc Lv8 + một hiệu ứng "đặc trưng". **Mọi hiệu ứng phạm vi đều có trần số mục tiêu** (không còn "toàn màn hình").

| Vũ khí gốc | Passive cặp | Tiến hóa | Chỉ số v2 | Hiệu ứng / Trần |
| :--- | :--- | :--- | :--- | :--- |
| Magma Broadsword | Book of Damage | ☀️ **Solar Flare Greatsword** | Nova 360° bán kính 3.0m mỗi 0.8s, **95 dmg** (≈119 DPS); **tổng cả Aura ≈131 DPS/mục tiêu (= ×1.5)** | Aura lửa 12 dmg/s lên **tối đa 12 mục tiêu** gần nhất trong 6m *(v1: 40 dmg/s toàn màn hình)* |
| Battle Axe | Heart of Life | 🪓 **Executioner's Greataxe** | Bổ nón 3.6m, **150 dmg**, CD 1.25s (**120 DPS = ×1.5**), Crit ×2.0 | **Execute**: quái thường (không Elite/Boss) dưới **10% HP** chết ngay *(v2.0: 15%, 157 DPS = ×1.96; v1: one-hit mọi quái thường)* |
| Twin Daggers | Feather of Speed | 🗡️ **Shadow Phantom Daggers** | **8×16** mỗi 0.50s (≈256 DPS) | Dash nhỏ 1.5m giữa các nhát; sàn CD 0.25s vẫn áp dụng |
| Dark Scythe | Iron Shield | 💀 **Reaper's Harvest** | **90 dmg**, CD 0.85s (**≈106 DPS = ×1.5**) | Hút máu **4% sát thương gây ra** *(v1: 10%)*, **thay thế** hồi 1.5 HP/kill của Scythe gốc, trần **8 HP/s** (TNT/quái triệu hồi không kích hoạt); Soul Shield hấp thụ 30 dmg mỗi 12s |
| Crystal Laser | Chronos Clock | ⚡ **Oblivion Death Ray** | 2 tia rộng **0.8m**, xoay **90°/s**, tick **0.20s × 22 dmg** (110 DPS khi mục tiêu nằm trong tia), tầm 7.5m | Xuyên thấu, **trần 15 mục tiêu/tick**. Khi quét trúng Boss: tia **hãm xoay còn 30°/s trong 0.6s** (đảm bảo ≈130 DPS lên Boss); không tick lên Boss quá 1 tia/lần. *(v2.0: 120°/s, tia không có độ rộng ⇒ DPS thực tế chỉ ~21 lên quái thường, ~41–49 lên Boss — thấp hơn cả Crystal Laser Lv8)* |
| Crossbow | Emerald of Wisdom | 🏹 **Gatling Repeater** | **5×13** mỗi 0.50s (≈130 DPS) | Bắn **quạt 5 mũi** (tổng góc 20°); mũi tên nổ lan 1.0m (40% dmg mũi tên), xuyên 4 với −15%/lần xuyên (ưu đãi tiến hóa so với Crossbow gốc) |

---

## 7. BẢNG CÂN BẰNG 12 LOÀI QUÁI VẬT (Normal)

*Máu/tốc độ ở bảng này là Normal. Áp hệ số mode (mục 2.1, 2.2, 12) cho Hard/Hardcore.*
*Tham chiếu: Steve = 4.0 m/s; Alex = 4.8 m/s.*

| Kẻ địch | Vai trò | HP gốc | HP @15:00 (N / H / HC) | Tốc độ gốc → @15:00 (N) | Tăng/phút | Dmg gốc | Rơi đồ | Cơ chế v2 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :--- | :--- |
| **Zombie** | Bầy đàn | 50 | 310 / 364 / 441 | 3.00 → 3.30 | +0.02 | 10 | Blue Gem (10 EXP), 5% 💎 | Xuất hiện thành cụm 4–8 |
| **Husk** | Trâu | 75 | 466 / 546 / 661 | 2.90 → 3.20 | +0.02 | 14 | Blue Gem, 6% 💎 | Kháng đẩy lùi +0.30 |
| **ZombieV2** | Giáp sắt | 70 | 435 / 509 / 617 | 3.10 → 3.40 | +0.02 | 12 | Blue Gem, 7% 💎 | **Giáp 15%** (giảm 15% sát thương nhận) |
| **HuskShovel** | Trâu xẻng | 95 | 590 / 691 / 837 | 3.00 → 3.30 | +0.02 | 16 | Green Gem (25 EXP), 8% 💎 | Đòn quét 1.5m, windup 0.4s |
| **Spider** | Đột kích | 30 | 186 / 218 / 264 | **4.30** → 4.75 | +0.03 | 8 | Blue Gem, 4% 💎 | Xuất hiện theo đàn 3–5; **nhanh nhưng giòn** |
| **Vex** | Bay xuyên chướng ngại | 25 | 155 / 182 / 220 | **4.50** → 4.95 | +0.03 | 10 | Blue Gem, 4% 💎 | Bay, bỏ qua địa hình; lướt mỗi 3s |
| **Phantom** | Săn mồi trên không | 35 | 217 / 255 / 308 | **4.20** → 4.65 | +0.03 | 12 | Blue Gem, 5% 💎 | **Swoop** có telegraph 0.6s |
| **Creeper** | Cảm tử nổ | 60 | 373 / 437 / 529 | 3.20 → 3.50 | +0.02 | **Nổ 35** *(v1: 45)* | Green Gem, 8% 💎 | Fuse 1.2s, bán kính 2.5m, nhấp nháy cảnh báo |
| **IceCreeper** | Nổ băng | 65 | 404 / 473 / 573 | 3.10 → 3.40 | +0.02 | **Nổ 30 + Slow 35%/2s** | Green Gem, 8% 💎 | Fuse 1.2s, bán kính 2.5m |
| **Slime** | Dẻo dai | 60 | 373 / 437 / 529 | 2.80 → 3.10 | +0.02 | 8 | Blue Gem, 5% 💎 | **Chia đôi khi chết** thành 2 Slime nhỏ (20% HP); **Slime con không rơi EXP/💎** (xem 7.1) |
| **Witch** | Ném độc tầm xa | 40 | 248 / 291 / 352 | 2.80 → 3.02 | +0.015 | 15 | Green Gem, 10% 💎 | Giữ khoảng cách 6m, ném bình mỗi 2.5s, vũng độc 5 dmg/s × 3s |
| **Evoker** | Pháp sư triệu hồi | 150 | 932 / 1,091 / 1,322 | 2.80 → 3.02 | +0.015 | 25 | Yellow Gem (50 EXP), 15% 💎 | Triệu hồi 3 Vex mỗi 12s (tối đa 6 sống cùng lúc); **Vex triệu hồi không rơi EXP/💎** (xem 7.1) |

*(Tốc độ @15:00 Hard/Hardcore = Normal × 1.05 / ×1.10. **v2.1:** chaser chậm tăng +0.02/phút ⇒ Zombie 15:00: 3.30 / 3.47 / 3.63 m/s; ZombieV2 Hardcore 3.74 m/s (v2.0: 3.91, chỉ cách Steve 2.4%) — biên kiting ≥ 6%.)*

**Hiệu chỉnh so với v1:** tốc độ gốc quái thường giảm 0.4–0.5 m/s để kiting khả thi ngay phút đầu cả Hardcore; sát thương chạm thực tế **tăng theo thời gian** (×1.75 vào phút 15); HP quái cuối trận cao hơn (Zombie 238 → 310 Normal), tăng dốc hơn ở Hard/Hardcore.

### 7.1. Quy tắc quái triệu hồi & phần thưởng (MỚI v2.1)
Chặn farm EXP bằng cách giữ Evoker sống / chia Slime / Sculk Burst:

| Nguồn | Rơi EXP/💎 | Tính tiến độ Elite | Hồi máu Scythe/Reaper | Tính vào Cap |
| :--- | :-: | :-: | :-: | :-: |
| Vex do Evoker triệu hồi | **Không** | Không | Không | **Có** (sub-cap 25) |
| Slime con (sau chia đôi) | **Không** (Slime gốc vẫn rơi 1 Blue Gem + 💎 như bảng) | Không | Không | **Có** (sub-cap 25) |
| Quái do Boss triệu hồi (Zombie của Golem, Sculk Burst) | **Không** | Không | Không | **Có** |
| Quái thường sinh từ WaveSpawner | Có | Có | Có | Có |

* Cap quái (150/180/220) là **cap toàn cục**: mọi loại quái đều đi qua cùng một Spawn Budget.
* Lý do: nếu Vex triệu hồi rơi EXP như Vex thường, người chơi giữ Evoker sống để giết Vex vô hạn; Sculk Burst của Warden còn kéo thẻ `Hearty Meal` liên tục.

---

## 8. HỆ THỐNG QUÁI TINH ANH (ELITE)

### 8.1. Sinh Elite (MỚI — bằng "ngân sách" thay cho % thuần)
| | Normal | Hard | Hardcore |
| :--- | :-: | :-: | :-: |
| Xác suất mỗi **lần roll** (roll mỗi 2s, xem 17 #2) | 3% | 5% | 8% |
| **Bắt đầu từ** | 2:00 | 1:30 | **1:00** |
| Khoảng cách tối thiểu giữa 2 Elite | 18s | 12s | 8s |
| **Số Elite sống tối đa cùng lúc** | 2 | 4 | 6 (8 trong Final Surge) |
| Affix | 1 | 1 | **2** (từ 8:00) |
| **Elite đầu tiên** | **Đảm bảo spawn** đúng mốc "Bắt đầu từ" (không roll) | Như Normal | Như Normal |

### 8.2. Chỉ số
* **Máu ×2.5** · **Sát thương ×1.30** *(v1: 1.25)* · **Kích thước ×1.20** · **Kháng đẩy lùi +0.25** · tốc độ **×1.0** (không nhanh hơn).
* Nhận diện (giữ nguyên v1): màu gốc, vòng `vfx_elite_aura_ring` (12 frames, PPU 32, sortingOrder 4, pivot {0.5, 0.2857}, lệch chân 0.0000), thanh máu tím `#A855F7`.

### 8.3. Affix (MỚI — chọn ngẫu nhiên)
| Affix | Hiệu ứng |
| :--- | :--- |
| **Swift** | **+12%** tốc độ *(v2.0: +20%)*. **Không** random lên Spider/Vex/Phantom; **không** đi cùng Berserk |
| **Armored** | Giảm 25% sát thương nhận |
| **Berserk** | +35% sát thương; <50% HP: +15% tốc độ *(không đi cùng Swift)* |
| **Regenerating** | Hồi **1%** Max HP/s *(v2.0: 2%)* (ngưng 2s khi bị trúng) |
| **Volatile** | Nổ khi chết (30 dmg, bán kính 2m, telegraph 0.5s) |

### 8.4. Phần thưởng (đã giảm lạm phát)
| | Giá trị |
| :--- | :--- |
| Luôn rơi | 1 Yellow Gem (**50 EXP**) + 1–3 💎 *(v1: 3–10)* |
| **Lucky Block Vàng** | **20% / 17% / 15%** (Normal/Hard/Hardcore) *(v1: 100%)* |
| Pity | **+10%** mỗi Elite không ra rương *(v2.0: +25% ⇒ tỉ lệ hiệu dụng ~42/40/39%, làm phẳng khác biệt giữa mode)*; **trần rương Elite / trận: 10 / 9 / 8** (N/H/HC) *(v2.0: 12)*; rương cách nhau ≥ 20s. Tỉ lệ hiệu dụng sau Pity ≈ **34% / 32% / 31%** |

> v1 nguyên nhân gây "thưởng quá tay khi khó": Hardcore sinh nhiều Elite nhất ⇒ nhiều TNT/Magnet/Steak nhất.

---

## 9. MINI-BOSS & BOSS (MỚI — có cơ chế, không còn chỉ là bao máu)

**Nguyên tắc:** HP đặt theo **thời gian hạ mục tiêu (TTK)**, giả định build điển hình và **uptime trúng đòn 70%**. Boss luôn **chậm hơn người chơi** (kiting khả thi) nhưng có chiêu **telegraph** bắt buộc né.

### 9.1. Bảng chỉ số

| | **Nether Golem I** (5:00) | **Nether Golem II — Enraged** (10:00) | **The Warden** (15:00) |
| :--- | :---: | :---: | :---: |
| **HP Normal** | **3,300** *(v1: 2,000)* | **9,600** *(v1: 3,500)* | **42,000** *(v1: 12,000)* |
| **HP Hard (×1.20)** | 3,960 | 11,520 | 50,400 |
| **HP Hardcore (×1.40)** | 4,620 | 13,440 | 58,800 |
| **Tốc độ** (Normal) | 2.2 m/s | 2.6 m/s | 2.0 m/s · Charge 5.5 m/s |
| **Sát thương chạm** | 30 | 40 | 55 |
| **TTK mục tiêu (Normal)** | 27–34s | 35–45s | 55–75s *(build có tiến hóa → 55s)* |
| **TTK v1 ước tính** | 10–16s | 4–9s | **1.8–6.5s** |
| **Phần thưởng** | Golden Chest **(+1 cấp cho tối đa 3 trang bị đang sở hữu; KHÔNG tiến hóa)** + 200 EXP + 15 💎 | Golden Chest **(đảm bảo tiến hóa nếu đủ điều kiện — hợp lệ vì sau 9:00)** + 400 EXP + 25 💎 | Chiến thắng + thưởng cuối ván |

*(Bosses nhân thêm hệ số Speed và Dmg theo mode; HP theo hàng ở trên. **v2.1:** HP mult boss 1.00/1.20/1.40 (v2.0: 1.00/1.25/1.55).)*

**Sát thương Boss (v2.1 chốt):** mọi chiêu của Boss/Mini-boss = chỉ số gốc × `DamMult_m` (1.00/1.15/1.30), **không** nhân hệ số thời gian `(1+0.05t)` — vì Boss xuất hiện ở thời điểm cố định. Nổ `Volatile` của Elite dùng hệ số `(1+0.025t)` như nổ Creeper. Quái do Boss triệu hồi **không rơi EXP/💎** (7.1). Khi Golem I/II xuất hiện, spawn quái thường giữ 100% (trần mật độ không tăng).

### 9.2. Chiêu thức
**Nether Golem I**
* **Ground Slam** — windup 1.0s (vòng cảnh báo), bán kính 3.0m, **35 dmg**, chu kỳ 5s.
* **Lava Throw** — 3 viên đạn cung, **20 dmg**, chu kỳ 8s.
* Hardcore: triệu hồi 6 Zombie mỗi 20s (không rơi gì, tính vào sub-cap).

**Nether Golem II (Enraged)**
* Slam bán kính 3.6m, **50 dmg**, chu kỳ 4.5s; Lava Throw 5 viên.
* **Enrage khi < 50% HP:** +20% tốc độ, để lại vệt lửa 12 dmg/s trong 4s.

**The Warden**
* **Melee Smash** — windup 1.0s, **60 dmg**.
* **Sonic Boom** — tia thẳng 9m, cast 1.5s, **45 dmg**, chu kỳ 7s.
* **Sculk Burst** — triệu hồi 8 quái (loại ngẫu nhiên, HP scale theo thời gian) mỗi 25s; **không rơi EXP/💎**.
* **Charge** — lao 5.5 m/s, telegraph 1.2s, **55 dmg** *(v2.0: 70 ⇒ Hardcore 91, one-shot Alex 85 HP; nay 71.5, Enrage 107)*, chu kỳ 14s.
* **Sculk Tendril Pull (MỚI — chống kite tầm xa)** — chỉ kích hoạt khi người chơi cách Warden **> 5.5m**; vùng chữ nhật telegraph 1.0s dưới chân người chơi; trúng thì **25 dmg** và kéo về cự ly 3.0m; chu kỳ 18s. Lý do: Crossbow (8m)/Laser (7m) kite ngoài tầm với mọi chiêu cận chiến, và Warden chỉ đi 2.0 m/s.
* **Phase 2 (<70% HP):** +10% tốc độ, Sonic Boom 2 tia song song.
* **Phase 3 (<35% HP):** +30% tốc độ, chu kỳ Boom −30%, Sculk Burst mỗi 18s.
* **Deafening Roar (Enrage timer):** sau **180s / 150s / 130s** (N/H/HC) kể từ khi xuất hiện *(v2.0: 150/120/100)* ⇒ +50% sát thương, **+35% tốc độ di chuyển**, Sculk Burst mỗi 10s — chống kiting vô hạn. Lý do nới: với uptime thực tế 50–70% và build chưa tiến hóa, TTK Hardcore có thể vượt 100s ngay cả ở kịch bản của chính v2.0 (build B: 104–115s). Mục tiêu đo: **P90 TTK < Enrage** (15.2).
* **Khi Warden xuất hiện:** spawn quái thường **giảm 60%**, trần mật độ ≤ 60% để trận đấu tập trung vào boss.

---

## 10. VẬT THỂ TƯƠNG TÁC & KINH TẾ

### 10.1. Rương May Mắn Vàng (Breakable Lucky Block)
* `IDamageable`, **60 HP**, **cần 3 đòn trúng** (i-frame 0.25s mỗi đòn để AoE không phá trong 1 frame).
* Hiện chữ: `CRACK! (1/3)` → `CRACK! (2/3)`.
* **Phần thưởng:** EXP = **25% EXP cần cho cấp kế tiếp** (**tối thiểu 5**; *v2.0: sàn 70 ⇒ ~4.7 lần nhu cầu Lv1*) + 2–5 💎 + ngẫu nhiên:

| Kết quả | v2 | v1 |
| :--- | :-: | :-: |
| **Steak** — hồi **30% Max HP** | 40% | 35% (50 HP cố định) |
| **Magnet** — hút sạch ngọc | 30% | 30% |
| **TNT** | **12%** | 25% |
| **Jackpot** — +15 💎 + EXP ×1.5 | 18% | 10% |

### 10.2. TNT (điều chỉnh)
* **Quái thường trong bán kính cố định 12.0m tính từ tâm người chơi** *(v2.0: "trong khung hình" — ultrawide 21:9/32:9 rộng hơn 30–50%)*: hủy diệt (99,999 dmg). **Giới hạn thưởng (v2.1):** gem EXP của quái bị TNT giết gộp thành 1 gem, tối đa **50% EXP cần cho cấp kế**; 💎 tối đa **15**; **không** kích hoạt hồi máu Dark Scythe/Reaper.
* **Elite:** nhận **35% Max HP**. **Boss:** miễn nhiễm.
* Xóa đạn của quái. **Không** 2 TNT trong vòng 45s (reroll thành Steak).

### 10.3. Kinh tế Emerald & Reroll
* 💎 reset về 0 sau mỗi ván (Zero-DB).
* **Reroll:** lần đầu mỗi cấp **MIỄN PHÍ**. Các lần sau: **2 → 3 → 5 → 8 → 12 💎**, **tối đa 5 lần tính phí/cấp**; **lần thứ 6 (chỉ Villager) = 18 💎** trước giảm giá *(v2.0 thiếu giá lần 6 ⇒ out-of-bounds; v1: 1→5 💎, không giới hạn lần)*.
* **Villager:** phí **−50%** (làm tròn lên: 1 / 2 / 3 / 4 / 6 / 9 💎), 1 lần miễn phí + tối đa **6 lần tính phí/cấp**, Emerald rơi **×1.5** *(v2.0: ×2 — kết hợp reroll rẻ cho phép reroll 3–4 lần ở mọi cấp, biến roguelite thành chọn đồ tất định)*.
* Ước tính 💎 mỗi ván ≈ 250–350 (Normal) ⇒ ~30–40 lượt reroll tính phí, đủ dùng nhưng không phá bể thẻ. **[CẦN ĐO]** — v2.0 chưa có bảng `EmeraldPerKill`/`DropProbability`; ước tính này chưa truy xuất được (xem 18).
* **Thưởng cuối ván (hiển thị/điểm):** ×1.0 / ×1.5 / ×2.2 *(giữ nguyên v1)*.

---

## 11. TIẾN TRÌNH TRẬN ĐẤU 15 PHÚT

### 11.1. Giai đoạn & loại quái (chung 3 mode)
| Mốc | Giai đoạn | Quái xuất hiện | Sự kiện |
| :--- | :--- | :--- | :--- |
| **00:00–03:00** | 1 · Khởi động | Zombie, Husk, Spider | Làm quen; **8–9 level-up** (Lv9–10 lúc 3:00 — v2.0 ghi "3–4" mâu thuẫn mục 13). |
| **03:00–06:00** | 2 · Gia tăng | + ZombieV2, HuskShovel, Creeper | **05:00 Nether Golem I** |
| **06:00–10:00** | 3 · Áp lực cao | + Slime, IceCreeper, Phantom, Vex | Quái bay ép di chuyển liên tục |
| **10:00–12:00** | 4 · Hỗn chiến | + Witch, Evoker | **10:00 Nether Golem II (Enraged)** |
| **12:00–15:00** | 5 · **Final Surge** | Toàn bộ 12 loài | HP quái **+35% dần lên**, trần Elite tăng — khớp thời điểm có tiến hóa |
| **15:00** | **Boss cuối** | **The Warden** | Xem 9.2 |

### 11.2. Mật độ (Normal / Hard / Hardcore)
| | 0:00 | 3:00 | 6:00 | 10:00 | 14:00 | 15:00 |
| :--- | :-: | :-: | :-: | :-: | :-: | :-: |
| **Normal** | 18 | 38 | 72 | 110 | 140 | 150 |
| **Hard** | 20 | 43 | 81 | 123 | 157 | 168 |
| **Hardcore** | 23 | 48 | 90 | 138 | 175 | 188 |

### 11.3. Sự kiện đặc biệt (MỚI — tạo "cảm nhận mode" ở đầu trận)
| Sự kiện | Giờ | Normal | Hard | Hardcore |
| :--- | :-: | :--- | :--- | :--- |
| **Pincer** (vòng vây Zombie) | 1:30 | — | 6 Zombie | **8 Zombie** *(v2.0: 10)* |
| **Swarm Rush** (đàn Spider/Vex) | 4:00 & 7:30 | 10 | 14 | 18 |
| **Siege** (Husk + Witch đồng loạt) | 12:30 | 8 | 12 | 16 |
| **Hướng spawn** | — | 2 phía | 3 phía | 4 phía (vây quanh) |

**Quy tắc Pincer (v2.1):** vòng spawn **bán kính 4.5m** (v2.0: 2m ⇒ 10 Zombie cách nhau 1.26m = tường kín, Zombie khép vòng trong 0.6s trong khi người chơi cần ~2s để hạ 1 con); **telegraph 1.5s** (vòng cảnh báo) trước khi Zombie xuất hiện; **chừa 1 góc thoát 60°** để người chơi đọc hướng mở và lách ra. Giữ Pincer ở Hard (6 Zombie, cùng quy tắc hình học) — núm vặn: bỏ Pincer khỏi Hard nếu Hard vỡ nhịp đầu trận (15.1).

---

## 12. 3 CHẾ ĐỘ KHÓ — BẢNG TỔNG

| Tham số | **Normal** | **Hard** | **Hardcore** | v1 (N/H/HC) |
| :--- | :-: | :-: | :-: | :-: |
| HP gốc (H0) | 1.00 | 1.10 | 1.22 | 1.0 / 1.25 / 1.5 |
| Độ dốc HP (s/phút) | 0.24 | 0.26 | 0.29 | 0.25 cố định |
| HP quái cuối (Zombie @15:00) | 310 | 364 | 441 | 238 / 297 / 356 |
| Tốc độ quái | ×1.00 | ×1.05 | ×1.10 | ×1.0 / 1.15 / 1.30 |
| Sát thương chạm (gốc) | ×1.00 | ×1.15 | ×1.30 | ×1.0 / 1.25 / 1.5 |
| **Scale sát thương theo thời gian** | ×(1+0.05t) | ×(1+0.05t) | ×(1+0.05t) | không có |
| Hệ số mật độ | ×1.00 | ×1.12 | ×1.25 | chung |
| Cap quái (**toàn cục**, gồm Elite + quái triệu hồi) | 150 | 180 | 220 | 150 / 200 / 250 |
| Elite: xác suất / bắt đầu | 3% / 2:00 | 5% / 1:30 | 8% / **1:00** | 4% / 8% / 15% |
| Elite sống tối đa | 2 | 4 | 6 | không giới hạn |
| Elite: rơi rương | 20% | 17% | 15% | 100% |
| HP Boss/Mini-boss | ×1.00 | ×1.20 | ×1.40 | ×1.0 / 1.3 / 1.65 *(v2.0: 1.00/1.25/1.55)* |
| Enrage Warden | 180s | 150s | 130s | không có *(v2.0: 150/120/100)* |
| I-frame chạm | 0.35s toàn cục | 0.35s toàn cục | 0.35s toàn cục | không có |
| EXP gem | ×1.00 | ×1.10 | ×1.25 | ×1.0 |
| Thưởng 💎 cuối ván | ×1.0 | ×1.5 | ×2.2 | giữ nguyên |
| Sự kiện Pincer/Swarm/Siege | nhẹ | vừa | nặng | không có |

**Cảm nhận theo thời gian (số của mô hình v2.0 ở mục 14 — chưa tái lập được, chỉ dùng để tham khảo xu hướng; xem cảnh báo 14):**
* **Đầu trận (≈1:00–3:00):** Hard = **1.3×** áp lực Normal; Hardcore = **1.8×** (v1: Hard 1.5×, Hardcore 2.3× nhưng *sau đó rơi mạnh*).
* **Cuối trận (≈15:00):** Hard = **1.4×**, Hardcore = **2.1×** (v1: Hard 2.1×, Hardcore 3.8× **nhưng tuyệt đối đã quá thấp**).
* **Áp lực không còn sụp đổ về cuối trận:** tỉ lệ cuối/đầu (phút 15 ÷ phút 3) = **×1.26 / ×1.37 / ×1.49** (Normal/Hard/Hardcore), trong khi v1 chỉ còn **×0.22 / ×0.30 / ×0.38**. Hardcore leo thang rõ nhất — đúng tinh thần "Ác Mộng".

---

## 13. QUY TRÌNH TỔNG QUÁT & KỲ VỌNG THEO MỐC (Normal, build điển hình)

| Mốc | Level (≈) | Vũ khí | Passive | Trạng thái |
| :--- | :-: | :--- | :--- | :--- |
| 1:00 | 4–5 | 1–2 vũ khí Lv1–2 | — | Thong thả, còn vài nhịp né |
| 3:00 | 9–10 | 3–4 vũ khí Lv1–3 | 1–2 passive | Đủ vũ khí, bắt đầu AoE |
| 5:00 | 15–16 | Lv ~3–4 | 2–3 passive Lv1–2 | Hạ Golem I (≈30s) |
| 10:00 | 33 | Lv ~5–6 | Lv ~3 | Hạ Golem II (≈40s) |
| 12:00–13:00 | 40–42 | Lv ~7–8 | Lv ~4 | **Tiến hóa đầu tiên** |
| 15:00 | **46–47 (45–46 lượt)** *(v2.0: 46–48)* | 1–2 vũ khí tiến hóa | 4 passive ≥ Lv4 | Warden ≈ 55–75s |

**EXP tích lũy cần theo công thức v2.1** (đây là EXP *cần có*, không phải EXP *kiếm được*; tốc độ thu EXP thực tế phải đo ở playtest):

| Mốc level | Lv5 | Lv9–10 | Lv16 | Lv33 | Lv40–42 | Lv46–47 |
| :--- | :-: | :-: | :-: | :-: | :-: | :-: |
| EXP tích lũy (v2.1) | 247 | 1,007–1,286 | 3,778 | 19,262 | 29,586–32,982 | 40,384–42,363 |
| EXP tích lũy (v2.0) | 216 | 971–1,262 | 3,999 | 22,799 | 36,058–40,496 | 50,273–52,908 |

---

## 14. KIỂM CHỨNG (MÔ HÌNH CÂN BẰNG VĨ MÔ)

> ⚠️ **Cảnh báo v2.1:** các bảng 14.1–14.4 là kết quả **mô hình v2.0, chưa được tái lập**: `EffDPS_player` không có công thức truy xuất, lịch lượt chọn `0/8/14/32/48/51` là *giả định* (không sinh ra từ nền kinh tế EXP; 51 lượt = Lv52, mâu thuẫn mục 13), và Build A (Daggers + Crossbow + Axe + Scythe) không thể do Steve tạo ra (Steve khởi đầu Broadsword, không có cơ chế bỏ vũ khí). Công thức HP quái, hits-to-die (14.3) và TTK Boss (14.4) **đã được tái tính khớp**; riêng **PI (14.1/14.2) không được dùng để khóa cân bằng**. Mô hình cần dựng lại **theo từng nhân vật** và xuất: level từng mốc, cấp từng vũ khí/passive, số mục tiêu trúng thực tế, DPS trước/sau Crit (xem 18).

**Giả định:** build mạnh "A" (Daggers + Crossbow + Axe + Scythe) và build yếu "B" (Broadsword + Laser + Scythe + Axe); số lượt chọn tại 0/3/5/10/14/15 phút = 0/8/14/32/48/51; passive Book & Clock cùng cấp; số mục tiêu AoE tăng theo mật độ (trần theo vũ khí); Steve làm nhân vật; HP quái trung bình gốc 62; boss uptime 70%. Kết quả là **chỉ số tương đối**, cần playtest để hiệu chỉnh số tuyệt đối.

### 14.1. Pressure Index (trung bình A/B) — v1 vs v2
| Phút | **v1 Normal** | **v1 Hard** | **v1 Hardcore** | **v2 Normal** | **v2 Hard** | **v2 Hardcore** |
| :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| 1 | 24.9 | 37.8 | 56.1 | 18.5 | 24.4 | 32.7 |
| 3 | 17.0 | 25.9 | 38.4 | 16.4 | 21.5 | 29.1 |
| 5 | 18.2 | 27.7 | 41.1 | 20.6 | 27.0 | 36.5 |
| 10 | 13.2 | 20.0 | 29.7 | 20.5 | 29.0 | 42.4 |
| 12 | 10.0 | 16.2 | 24.0 | 19.9 | 28.2 | 41.4 |
| 14 | 4.1 | 7.6 | 11.2 | 18.1 | 25.7 | 37.8 |
| 15 | **3.8** | **7.8** | **14.4** | **20.7** | **29.4** | **43.3** |
| **Cuối/Đầu (15÷3)** | **0.22** | **0.30** | **0.38** | **1.26** | **1.37** | **1.49** |

> Đọc: v1 Normal giảm xuống chỉ còn **22%** áp lực ban đầu; Hardcore cuối trận (14.4) **thấp hơn** Normal phút 3 (17.0). v2: Normal dao động chỉ ±12% quanh mức đầu trận; Hard/Hardcore leo thang nhẹ (đến +40–50%) rồi giữ ổn định qua Final Surge — cao trào có chủ đích, không sụp đổ.

### 14.2. Tăng trưởng sức mạnh (eff. DPS) vs tải quái (mật độ × HP) — Normal
| | Người chơi (A / B) | Tải quái |
| :--- | :-: | :-: |
| v1 | ×176 / ×129 | ×20 |
| v2 | ×27 / ×36 | ×30 |

### 14.3. Số đòn để chết (Zombie, Steve, build sống sót đủ) — Normal / Hard / Hardcore
| | Phút 0 | Phút 15 |
| :--- | :-: | :-: |
| v1 | 13.3 / 10.7 / 8.9 | **36.7 / 29.3 / 24.4** |
| v2 | 13.3 / 11.6 / 10.3 | **17.1 / 14.9 / 13.2** |

### 14.4. Thời gian hạ Boss (build A / B, uptime 70%)
| | v1 | v2 (Normal) | v2.1 (Hardcore, HP ×1.40) |
| :--- | :-: | :-: | :-: |
| Golem I | 9.7s / 16.0s | 27s / 34s | 38s / 48s |
| Golem II | 4.0s / 9.0s | 35s / 45s | 50s / 63s |
| The Warden | 1.8s / 6.5s | 55s / 74s | 77s / 104s |

*(v2.0 Hardcore: 42/53 · 55/70 · 85/115s; v2.1 = v2.0 × 1.40/1.55 vì TTK tỉ lệ thuận HP.)*

### 14.5. Độ nhạy của TTK Warden (MỚI — số học đơn giản, kiểm chứng được)
Kịch bản bất lợi: EXP −25%, build thiên sống sót làm DPS −20%, uptime lên Boss 50% thay vì 70% ⇒ DPS hiệu dụng ×0.60 và TTK ×(1/0.60)×(0.70/0.50) = **×2.33**.

| Mode (build A / B) | TTK gốc | TTK kịch bản bất lợi | Enrage v2.1 | Kết quả |
| :--- | :-: | :-: | :-: | :--- |
| Normal | 55 / 74s | 128 / 173s | 180s | Còn trước Enrage (B sát nút) |
| Hard | 67 / 91s | 157 / 212s | 150s | **Chạm Enrage** |
| Hardcore | 77 / 104s | 179 / 243s | 130s | **Chạm Enrage** |

**Kết luận:** kết quả rất nhạy với (1) lượng nâng cấp thực nhận và (2) uptime lên Boss. Không dùng uptime 70% làm giả định cố định khi đặt HP Warden — benchmark ở uptime 50–70% và đặt KPI P90 TTK < Enrage (15.2).

---

## 15. NÚM VẶN & KPI PLAYTEST

### 15.1. Núm vặn (chỉnh theo thứ tự ưu tiên)
| Hiện tượng | Chỉnh |
| :--- | :--- |
| Cuối trận vẫn quá dễ | Tăng độ dốc `s` (+0.02), hoặc `Surge` 0.35 → 0.45, hoặc `DM` +0.05 |
| Cuối trận quá khó / không kịp dọn | Giảm `Surge`, giảm `DM` Hard/Hardcore, tăng DPS vũ khí +5% |
| Phút 0–2 quá dễ ở Hardcore | Tăng `DM` đầu trận, hoặc tăng số quái Pincer; **không** tăng tốc quái |
| Phút 0–2 quá khó ở Hardcore | Giảm `DM` hoặc dời Elite đầu lên 1:30 |
| Một build chiếm >35% lượt chơi thắng | Hạ `Role` vũ khí đó 5–10% |
| Boss quá nhanh/chậm | Chỉnh HP boss (TTK tỉ lệ thuận HP) |
| Level quá chậm ở Hardcore | Tăng EXP mult Hardcore 1.25 → 1.35 (chỉ khi EXP/phút Hardcore < 85% Normal) |
| Median Lv@15:00 Normal < 42 | Giữ `20×Lv^1.20`; nếu vẫn thấp: hạ hệ số 20 → 18, hoặc đưa vũ khí tối đa Lv8 → Lv7 |
| Median Lv@15:00 Normal > 49 | Quay về số mũ 1.35 hoặc tăng hệ số 20 → 22 |
| Warden chạm Enrage > 15% / 30% / 45% (N/H/HC) | Nới Enrage +20s, hoặc hạ HP mult boss 1.20/1.40 → 1.15/1.30 |
| Hard vỡ nhịp đầu trận | Bỏ Pincer khỏi Hard (chỉ Hardcore) |
| Ván có tiến hóa trước 9:00 | Khóa lại điều kiện ở mục 6 |

### 15.2. KPI mục tiêu (người chơi trung bình, sau ~10 ván)
| KPI | Normal | Hard | Hardcore |
| :--- | :-: | :-: | :-: |
| Tỉ lệ thắng | 65–80% | 35–50% | 10–20% (nhóm kỹ năng cao 30–40%) |
| Tỉ lệ chết trước 5:00 | < 5% | 8–12% | 15–25% |
| % Max HP mất **ròng** mỗi phút (đã trừ Regen/hồi máu) (đầu → cuối) | 8% → 8–10% | 12% → 12–14% | 18% → 18–22% |
| **Gross Damage Taken / Max HP mỗi phút** *(MỚI; ước tính tạm, hiệu chỉnh sau 20 ván đầu)* | 30–45% | 45–65% | 65–90% |
| Tiến hóa đầu tiên sớm nhất | ≥ 9:00 (không ngoại lệ) | ≥ 9:00 | ≥ 9:00 |
| % ván chạm Enrage Warden | ≤ 15% | ≤ 30% | ≤ 45% |
| P90 TTK Warden / Enrage | < 1.0 | < 1.0 | < 1.0 |
| EXP/phút so với Normal (build tương đương, trước Elite/Boss) | 100% | 85–100% | 85–100% |
| Một nhân vật/vũ khí chiếm tỉ lệ thắng | ≤ 35% tổng (đặc biệt **Alex + Twin Daggers**) | ≤ 35% | ≤ 35% |
| Thời gian đạt tiến hóa đầu tiên | 12:30 | 13:00 | 13:30 |
| Số TNT nhặt mỗi ván | 1–3 | 1–3 | 1–3 |
| Phần trăm thời gian di chuyển liên tục | > 70% ở cả 3 mode (chỉ số "kiting còn sống") |

### 15.3. Danh sách việc cần làm (Dev)
1. Cài `ContactDamage(t)`, **i-frame toàn cục 0.35s + hệ số cộng dồn bị vây (2.5)**, trần Armor/**CDR 30%**/Speed/Crit/**Bonus Damage 60%** trong `PlayerStats`.
2. Thay bảng vũ khí + passive bằng bảng mục 4–5 (ScriptableObject).
3. Cài Elite Budget (giãn cách, alive cap, affix) và giảm tỉ lệ rơi rương.
4. Cài cơ chế Boss (telegraph, phase, enrage) — hiện tại boss chỉ là bao máu.
5. Cài sự kiện Pincer/Swarm/Siege + hướng spawn theo mode.
6. Cài bể thẻ dự phòng (5.1) và Evolution Pity (5.2).
7. Gắn telemetry: DPS người chơi theo phút, Pressure Index (mật độ × HP / DPS), thời gian hạ boss, tỉ lệ chết theo phút/mode.
8. **(v2.1)** Telemetry bổ sung (JSON cuối ván hoặc mỗi 60s): `levelByMinute[0..15]`, `unpickedGemsCount/unpickedExpValue`, `avgTargetsHitPerCast[weaponId]`, `damageTakenBySource[enemyType/bossSkill]`, `blockedDamageByIFrame`, `bossTTK`, `uptimePerWeapon[weaponId]`, `wardenEnrageTriggered`, `FirstEvolutionTimestamp`, `DeathTimestampHistogram` (đặc biệt 1:30–1:50 và 8:00–10:00), `KillerEnemyAffixes`, `p95FrameTimeMs`, `maxActiveEnemies`, `maxActiveGems`. Log riêng Clear/Survival/Boss Pressure (2.7).
9. **(v2.1)** `ContactDamage` toàn cục 0.35s + hệ số cộng dồn bị vây; khóa riêng 0.25s cho nổ/độc/đạn/chiêu Boss; Regen ngưng 1.0s sau khi trúng đòn.
10. **(v2.1)** Đưa vào `Evolution gating` (≥ 9:00, tối đa 2/ván), Pity viết lại + thẻ Swap, Elite roll 2s + Elite đầu tiên đảm bảo, quái triệu hồi không loot, cap toàn cục + sub-cap 25, TNT giới hạn thưởng, Sculk Tendril Pull, `TimeScaleManager`.
11. **(v2.1)** Dựng lại mô hình mục 14 **theo từng nhân vật** (script có thể tái lập); mỗi vũ khí có bảng DPS theo 1/2/3/4 mục tiêu.

---

## 16. RÀNG BUỘC KỸ THUẬT & QUY TẮC PHÁT TRIỂN DÀNH CHO AGENT (v1; v2.1 sửa quy tắc 3, thêm 6–10)

1. **Vật lý di chuyển:** bắt buộc `Rigidbody2D.linearVelocity`; không `AddForce` hay chỉnh `transform.position` thô; `gravityScale = 0`.
2. **Sprite PPU = 32:** Filter Mode `Point (no filter)`, Compression `None`.
3. **Quản lý TimeScale (v2.1 sửa):** **chỉ một class duy nhất** (`TimeScaleManager` hoặc `InGamePauseManager`) được ghi `Time.timeScale`, qua cơ chế đếm tham chiếu (`PauseToken` / `pauseLockCount`). Hàm reset `Time.timeScale = 1f` chỉ gọi trong `SceneManager.sceneLoaded`. **Cấm** đặt `Time.timeScale` trong `Awake()` của các Manager khác — một Manager được `Instantiate`/`SetActive` lúc đang mở bảng Level-Up/Pause sẽ âm thầm bỏ pause, để quái đánh người chơi sau bảng UI.
4. **TextMeshPro:** không dùng `UnityEngine.UI.Text`; dùng `TextMeshProUGUI` + font `VCR_OSD_MONO SDF.asset`.
5. **Tránh rác Scene:** mọi script sinh chữ bay (`DamageNumber.cs`) phải có guard `if (!Application.isPlaying) return;`.
6. **(v2.1) Giới hạn thực thể:** cap quái là cap toàn cục gồm Elite, Vex/Slime triệu hồi, quái Boss triệu hồi; mọi quái đi qua cùng một Spawn Budget. Ước tính peak Hardcore có thể ~260–280 Rigidbody2D nếu không gom cap.
7. **(v2.1) Vật lý quái:** giữ `Rigidbody2D` Kinematic (vẫn dùng `linearVelocity`), **tắt va chạm Enemy–Enemy** trong Layer Collision Matrix; tách bầy bằng separation/boids trên Spatial Grid (ô ~4m), AI cập nhật 10 Hz theo nhóm.
8. **(v2.1) Truy vấn mục tiêu:** dùng `Physics2D.OverlapCircleNonAlloc`/`ContactFilter2D` với mảng cấp phát sẵn 64 phần tử; không `OverlapCircleAll` mỗi tick.
9. **(v2.1) Pool:** Object Pool cho Enemy, Projectile, Pickup, DamageNumber. `DamageNumber` của DoT/Beam gộp theo 0.5s/mục tiêu; tối đa 60 instance đang hoạt động (ghi đè cũ nhất).
10. **(v2.1) Gem:** tự gộp (auto-merge) gem khi số gem trên bản đồ > 250.

---

## 17. QUYẾT ĐỊNH TẠM CHO CÁC CÂU HỎI MỞ (v2.1)

Hai review đều nêu các điểm tài liệu không trả lời được; dưới đây là **quyết định mặc định để dev có thể code ngay**. Gắn nhãn **[GIẢ ĐỊNH]** = tác giả cần xác nhận, đổi được bằng config mà không đổi kiến trúc.

| # | Câu hỏi | Quyết định tạm |
| :-: | :--- | :--- |
| 1 | `Density` là gì, và bù quái thế nào? | `Density` = số quái sống đồng thời mục tiêu. WaveSpawner kiểm tra mỗi **0.5s** và bổ sung phần thiếu, tối đa **8% Density (tối thiểu 2 con)** mỗi lần — giết quái **không** sinh bù tức thì (tránh farm EXP vô hạn theo DPS). **[GIẢ ĐỊNH]** |
| 2 | Elite roll khi nào? | Roll mỗi **2 giây** từ mốc bắt đầu của mode (3/5/8%); **Elite đầu tiên đảm bảo** tại mốc bắt đầu, các Elite sau mới roll; tuân theo khoảng cách tối thiểu và alive cap. **[GIẢ ĐỊNH]** |
| 3 | Mỗi level-up mấy thẻ? Skip? | **3 thẻ**; thẻ mở trang bị mới và nâng cấp cùng bể, trọng số bằng nhau (Pity nhân thêm); **không có Skip**; không bỏ được vũ khí khởi đầu. **[GIẢ ĐỊNH]** |
| 4 | Vex/Slime con/quái Boss triệu hồi có loot, có tính cap? | Không loot; **có** tính cap toàn cục; sub-cap 25 (mục 7.1). |
| 5 | Sát thương Boss/Elite có nhân `(1+0.05t)`? | Boss: chỉ `DamMult_m`, không nhân thời gian. Elite contact: như quái thường × 1.30. Nổ `Volatile`: `(1+0.025t)`. |
| 6 | Hình học tiến hóa còn thiếu | Solar Flare Nova bán kính **3.0m** (Aura 6m); Reaper's Harvest hút máu **thay thế** hồi theo kill, trần 8 HP/s; Gatling: **quạt 5 mũi**, tổng góc 20°, xuyên 4, −15%/lần xuyên. **[GIẢ ĐỊNH]** |
| 7 | Giá reroll lần 6 (Villager)? | **18 💎** (9 💎 sau giảm giá). |
| 8 | Passive Lv5 có giải phóng slot? | **Không** — vẫn chiếm slot. |
| 9 | Spawn khi Golem I/II xuất hiện? | Giữ 100% (chỉ Warden giảm 60%). |
| 10 | Phiên bản Unity `6000.4.10f1` | **Cần xác nhận**: review lưu ý nhánh LTS chuẩn của Unity 6 là `6000.0.x` / `6000.3.x`; xác nhận có dùng bản custom/beta không. Chưa đổi ở mục 1. |

---

## 18. VIỆC CHƯA VÁ (BACKLOG SAU V2.1)

Các mục dưới đây **không** sửa trong v2.1 vì cần dữ liệu playtest hoặc là ý tưởng thiết kế (không phải lỗi):

1. **Dựng lại mô hình cân bằng** (mục 14): script tái lập được, `EffDPS_player` có công thức, tách theo nhân vật, xuất level/cấp vũ khí/số mục tiêu theo mốc; thay PI bằng Clear/Survival/Boss Pressure.
2. **Bảng DPS 1/2/3/4 mục tiêu** cho từng vũ khí (hiện chỉ có Crossbow); đo `TargetsHitPerAttack` thực tế.
3. **Bảng kinh tế Emerald:** `EmeraldPerKill`, `DropProbability`, `EliteReward`, `ChestReward` để tính lại ngân sách reroll.
4. **Mô hình thu EXP** (EXP/phút theo mode); xác nhận hệ số EXP Hard/Hardcore (hiện 1.10/1.25; review gợi ý có thể cần 1.15/1.35).
5. **Golem II chưa khác Golem I về cơ chế** — ý tưởng: 2 `Magma Totem` cố định (1,200 HP, dây nối giảm 50% sát thương lên Golem II) ở mốc 50% HP.
6. **Cửa sổ phản công Boss** (thưởng kỹ năng né) — ý tưởng: Warden Charge đâm vào địa hình ⇒ stagger 2s, nhận +20% sát thương; Sonic Boom khóa hướng sau telegraph.
7. **Elite Execute giới hạn HP** (nếu Executioner vẫn vượt ngân sách sau playtest): trần 150 HP/mục tiêu.
8. **Soft collision / separation** để giảm nguy cơ "bức tường quái" ở Hardcore phút 14–15.
9. **Hard có cần Pincer?** Theo dõi tỉ lệ thắng; núm vặn ở 15.1.
10. **Unity version** (mục 17 #10).

---

## PHỤ LỤC A — HẰNG SỐ CẤU HÌNH TỔNG HỢP (COPY VÀO CONFIG) — v2.1

```json
{
  "version": "2.1",
  "hpScale": {
    "Normal":   { "H0": 1.00, "slope": 0.24 },
    "Hard":     { "H0": 1.10, "slope": 0.26 },
    "Hardcore": { "H0": 1.22, "slope": 0.29 },
    "surge": { "startMin": 12, "endMin": 15, "bonus": 0.35 }
  },
  "speedMult": { "Normal": 1.00, "Hard": 1.05, "Hardcore": 1.10 },
  "chaserSpeedPerMin": { "Zombie": 0.02, "Husk": 0.02, "ZombieV2": 0.02, "HuskShovel": 0.02 },
  "contactDmg": {
    "perMinute": 0.05, "burstPerMinute": 0.025,
    "mult": { "Normal": 1.00, "Hard": 1.15, "Hardcore": 1.30 },
    "iFrameSec": 0.35, "iFrameScope": "global",
    "stackPerExtraToucher": 0.25, "stackMaxExtra": 4,
    "nonContactSourceLockSec": 0.25
  },
  "regen": { "pauseAfterHitSec": 1.0 },
  "density": {
    "base": [[0,18],[3,38],[6,72],[10,110],[14,140],[15,150]],
    "mult": { "Normal": 1.00, "Hard": 1.12, "Hardcore": 1.25 },
    "cap":  { "Normal": 150,  "Hard": 180,  "Hardcore": 220 },
    "capIsGlobal": true, "summonSubCap": 25,
    "refillTickSec": 0.5, "refillMaxPctPerTick": 0.08, "refillMinPerTick": 2
  },
  "elite": {
    "chance": { "Normal": 0.03, "Hard": 0.05, "Hardcore": 0.08 },
    "rollIntervalSec": 2, "firstEliteGuaranteed": true,
    "startSec": { "Normal": 120, "Hard": 90, "Hardcore": 60 },
    "minGapSec": { "Normal": 18, "Hard": 12, "Hardcore": 8 },
    "aliveCap": { "Normal": 2, "Hard": 4, "Hardcore": 6 },
    "chestChance": { "Normal": 0.20, "Hard": 0.17, "Hardcore": 0.15 },
    "chestPityStep": 0.10,
    "chestCapPerRun": { "Normal": 10, "Hard": 9, "Hardcore": 8 },
    "chestMinGapSec": 20,
    "hpMult": 2.5, "dmgMult": 1.30, "scale": 1.20, "knockbackResist": 0.25,
    "affix": { "swiftSpeed": 0.12, "regenPctPerSec": 0.01,
               "swiftExcludesEnemies": ["Spider", "Vex", "Phantom"],
               "mutex": [["Swift", "Berserk"]] }
  },
  "boss": {
    "hpMult": { "Normal": 1.00, "Hard": 1.20, "Hardcore": 1.40 },
    "damageUsesTimeScale": false,
    "summonsDropLoot": false,
    "golem1": { "hp": 3300,  "speed": 2.2, "contactDmg": 30, "chest": { "levelUpItems": 3, "evolves": false } },
    "golem2": { "hp": 9600,  "speed": 2.6, "contactDmg": 40, "chest": { "evolves": true } },
    "warden": { "hp": 42000, "speed": 2.0, "charge": 5.5, "chargeDmg": 55, "contactDmg": 55,
                "enrageSec": { "Normal": 180, "Hard": 150, "Hardcore": 130 },
                "enrageDmgBonus": 0.5, "enrageSpeedBonus": 0.35,
                "tendrilPull": { "minDistM": 5.5, "telegraphSec": 1.0, "dmg": 25, "pullToM": 3.0, "cycleSec": 18 } }
  },
  "events": {
    "pincer": { "timeSec": 90, "radiusM": 4.5, "telegraphSec": 1.5, "escapeGapDeg": 60,
                "count": { "Normal": 0, "Hard": 6, "Hardcore": 8 } }
  },
  "caps": { "armor": 0.35, "cdr": 0.30, "moveSpeedBonus": 0.40, "crit": 0.60, "bonusDamage": 0.60 },
  "expCurve": { "base": 20, "exponent": 1.20 },
  "expMult": { "Normal": 1.00, "Hard": 1.10, "Hardcore": 1.25 },
  "targetPicksAt15min": 46,
  "evolution": {
    "minTimeSec": 540, "maxPerRun": 2,
    "pityStartSec": 480, "pityGuaranteeWithinLevelUps": 3,
    "passiveLv5OccupiesSlot": true
  },
  "luckyBlock": { "expPctOfNextLevel": 0.25, "expMin": 5, "hits": 3, "iFrameSec": 0.25 },
  "tnt": { "radiusM": 12.0, "expCapPctOfNextLevel": 0.5, "emeraldCap": 15,
           "triggersLifesteal": false, "minGapSec": 45 },
  "backupPool": { "bountyMarkMaxPerRun": 3, "scholarsTomeStacks": false },
  "reroll": { "firstFree": true, "costs": [2,3,5,8,12,18], "maxPaidPerLevel": 5,
              "villager": { "discount": 0.5, "maxPaidPerLevel": 6, "emeraldMult": 1.5 } }
}
```

---

*Hết tài liệu — v2.1 vá v2.0 theo review; v2.0 vẫn thay thế hoàn toàn v1 về mọi con số cân bằng; các quy ước kỹ thuật (mục 16) đã cập nhật quy tắc 3 và bổ sung 6–10; định danh asset không thay đổi.*
