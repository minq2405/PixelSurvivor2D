# PIXEL SURVIVOR - BẢN ĐỒ TỔNG HỢP DỰ ÁN TOÀN DIỆN (MASTER PROJECT INDEX)

Tài liệu này đóng vai trò là **ngôi nhà chung duy nhất (Single Source of Truth)** gom toàn bộ kiến thức, lịch sử các cuộc hội thoại (conversations), kiến trúc kỹ thuật và tiến độ của dự án Pixel Survivor về một mối.

---

## 1. Giải Pháp Chấm Dứt Tình Trạng Phân Tán Chat & Mất Context

Lý do trước đây bạn phải tạo nhiều chat là vì sợ **AI bị đầy bộ nhớ và quên context cũ**. Tuy nhiên, điều này lại khiến các quyết định kỹ thuật bị xé nhỏ ra nhiều nơi. Để giải quyết dứt điểm:

### Cách 1: Đặt Workspace Mặc Định Trong Antigravity IDE (Khuyên Dùng)
1. Trong cửa sổ Antigravity IDE, chọn menu: **`File` $\rightarrow$ `Open Folder...`** (hoặc `Open Workspace`).
2. Điều hướng và chọn thư mục: `C:\Users\PC\OneDrive\Desktop\Unity\ProjectFinalPru`
3. **Lợi ích:** 
   - Antigravity sẽ gán cố định toàn bộ các đoạn chat vào dự án này.
   - AI luôn tự động đọc cây thư mục, file code, asset và Memory Bank mà bạn không cần phải giải thích lại đường dẫn hay gõ lệnh nhắc nhở.

### Cách 2: Dùng Lệnh `/compact` Khi Chat Bắt Đầu Dài (Không Cần Tạo Chat Mới)
- Trong Antigravity, khi một đoạn chat đã trao đổi nhiều tin nhắn, bạn **không cần tạo chat mới**.
- Chỉ cần gõ lệnh:
  ```text
  /compact
  ```
- Hệ thống sẽ tự động nén toàn bộ lịch sử thành bản tóm tắt siêu ngắn gọn, dọn sạch token rác và giữ lại 100% các quyết định kỹ thuật, bug đã sửa và công việc tiếp theo. Bạn có thể tiếp tục làm việc trên đúng 1 cuộc hội thoại duy nhất xuyên suốt nhiều tháng!

### Cách 3: Hệ Thống Auto-Context Rule Vừa Được Cài Đặt
Tôi đã tạo một Global Rule vĩnh viễn tại `C:\Users\PC\.gemini\config\rules\pixel_survivor_project_context.md`.
- **Hiệu quả:** Kể từ bây giờ, bất kỳ khi nào bạn mở bất kỳ đoạn chat nào và nhắc đến "Pixel Survivor", AI sẽ **tự động đọc Memory Bank và nắm rõ 100% tình trạng dự án ngay từ câu chào đầu tiên**, không bao giờ bị mất trí nhớ nữa!

---

## 2. Nhật Ký Lịch Sử Các Phiên Chat (Conversation Index)

Dưới đây là liên kết trực tiếp tới các phiên làm việc chính của bạn (nhấp vào link để mở lại phiên làm việc tương ứng trong Antigravity):

| Phiên Làm Việc (Conversation Link) | Chủ Đề & Nội Dung Chính Đã Thực Hiện |
| :--- | :--- |
| [Phiên hiện tại: APK Build, Sửa Boss, Nhạc BGM](conversation://4b83f663-b229-4c42-9868-df57b5e747ff) | Build APK v1-v4, Sửa lỗi Mini-Boss tàng hình trên mobile, Chuyển sprite sang `Resources/`, Tối ưu FPS 60-90, Sửa nút Pause, Nhạc C418 ElevenLabs. |
| [Phiên âm thanh & phát triển game](conversation://c9ef27bb-3c1a-4b43-852c-a7167b316911) | Thiết kế hệ thống âm thanh, BGM/SFX và vai trò phát triển game. |
| [Phiên đọc Memory Bank & chuẩn bị Giai đoạn](conversation://6e45481f-a098-496c-983e-f83bca83fb84) | Rà soát hệ thống Memory Bank và đồng bộ trạng thái. |
| [Phiên tìm kiếm công cụ tạo SFX](conversation://0a5ce358-417b-4165-8570-308071581c66) | Khảo sát AI sinh âm thanh hiệu ứng (SFX) cho vũ khí và quái vật. |
| [Phiên phân tích ảnh & asset tải về](conversation://00d54944-b762-4cfe-9daf-824ab8ba1ab0) | Xử lý format JSON và asset đồ họa từ Downloads. |
| [Phiên triển khai Giai đoạn cốt lõi](conversation://b35dee62-a2c0-47da-aeb8-37edcb81d9c4) | Lập trình logic WaveSpawner, quái vật, vũ khí và UI nâng cấp. |
| [Phiên thiết kế quái Elite & Affixes](conversation://1e076ae9-95ed-4748-8eaf-58323b5c669e) | Xây dựng hệ thống quái tinh anh (Elite Affixes, vòng hào quang, nhân chỉ số). |

---

## 3. Bản Đồ Thư Mục & Tài Nguyên Quan Trọng

- **Thư mục Unity chính:** `C:\Users\PC\OneDrive\Desktop\Unity\ProjectFinalPru`
- **Kho Git Mirror:** `C:\Users\PC\OneDrive\Desktop\git_repo\PixelSurvivor2D`
- **Thư mục Báo Cáo & Tài Liệu:** `C:\Users\PC\OneDrive\Desktop\AntigravityAgent\PixelSurvivor\`
- **Bộ nhớ Memory Bank:** `C:\Users\PC\OneDrive\Desktop\Unity\ProjectFinalPru\memory-bank\`

### Các File Kỹ Thuật Đã Hoàn Thiện & Xác Minh:
1. **Quản lý quái & Dòng thời gian 18 phút:** `Assets/PixelSurvivor/Scripts/Combat/WaveSpawner.cs`
2. **Đấu trường Boss & Đóng băng Camera:** `Assets/PixelSurvivor/Scripts/Combat/BossArenaManager.cs`
3. **9 Boss & Mini-Boss (Nạp Resources động):** `Assets/PixelSurvivor/Scripts/Combat/*Boss.cs`
4. **Điều khiển Joystick Mobile (100% full speed):** `Assets/PixelSurvivor/Scripts/Mobile/MobileJoystick.cs`
5. **Nút Pause In-Game & Modal Cài đặt:** `Assets/PixelSurvivor/Scripts/UI/InGamePauseManager.cs`
6. **Hệ thống Nâng cấp & Tiến hóa:** `Assets/PixelSurvivor/Scripts/UI/LevelUpManager.cs`
7. **Âm thanh & Nhạc BGM mới (C418 style):** `Assets/PixelSurvivor/Resources/Audio/BGM/bgm_battle.wav`
8. **File cài đặt APK v4:** `C:\Users\PC\OneDrive\Desktop\Unity\ProjectFinalPru\PixelSurvivorAPK_v4\PixelSurvivor_v4.apk`
