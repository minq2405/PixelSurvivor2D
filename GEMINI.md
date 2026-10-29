# Project Rules for ProjectFinalPru

## Unity MCP Execution & Manual Guidance Mode
Khi người dùng yêu cầu thao tác Unity (bằng MCP tools):
1. **Thực thi qua MCP**: Vẫn tự động thực thi các thao tác trên Unity Editor qua MCP tools như bình thường (tạo/sửa GameObject, component, transform, physics, v.v.).
2. **Nội dung phản hồi**: Không chỉ dừng lại ở việc báo cáo các lệnh MCP đã chạy. Thay vào đó, phản hồi phải tập trung **hướng dẫn chi tiết từng bước cách chỉnh thủ công trên giao diện Unity Editor** (chọn GameObject nào ở Hierarchy, xem Component nào ở Inspector, chỉnh thuộc tính gì và điền giá trị bao nhiêu).

## Memory Bank Protocol
Dự án PixelSurvivor sử dụng hệ thống Memory Bank để duy trì bối cảnh và quy tắc phát triển:
1. **Khởi động phiên (Startup):** Ở đầu mỗi cuộc trò chuyện hoặc khi nhận task mới, luôn đọc `memory-bank/activeContext.md` và `memory-bank/decisionLog.md` trước khi trả lời hoặc thực thi.
2. **Tuân thủ Quyết định (Decision Compliance):** Tuyệt đối không làm trái các quyết định kiến trúc đã khóa trong `memory-bank/decisionLog.md` (PPU = 32, URP 2D, Rigidbody2D, bảng màu UI, tiếng Anh in-game, không sửa file scene thô...).
3. **Cập nhật liên tục (Continuous Update):**
   - Khi có quyết định kỹ thuật mới được chốt: Ghi ngay vào `memory-bank/decisionLog.md`.
   - Khi hoàn thành tính năng hoặc chuyển giao task: Cập nhật `memory-bank/progress.md` và `memory-bank/activeContext.md`.
