# Brute Call

- **Mã Thẻ Bài**: `brute_call`
- **Số sao**: 3
- **Loại Thẻ**: [`ability`](../../../../04_abilities.md)
- **Chủng Tộc Chính**: **Natureborn**
- **Tộc Nhánh**: **Goblin** (định danh kỹ thuật: `goblin`)
- **Thẻ Nhân Vật Yêu Cầu**: [Goblin Shaman](../goblin_shaman.md)
- **Thẻ Nhân Vật Được Triệu Hồi**: [Goblin Brute](../goblin_brute.md)

## Mô Tả Kỹ Năng

Khi Goblin Shaman gặp nguy hiểm, cô triệu gọi Goblin Brute xông ra bảo vệ mình. Goblin Brute luôn xuất hiện ở vị trí liền kề bên trái hoặc bên phải cô. Với thân hình đồ sộ và bước chân nặng nề, Goblin Brute có thể giẫm chết một Goblin 1 hoặc 2 sao đang đứng tại một trong hai vị trí đó trong lúc tiến vào chiến trường.

Nếu không có Goblin 1 hoặc 2 sao hợp lệ đứng cạnh Goblin Shaman nhưng có vị trí liền kề đang trống, Goblin Brute vẫn được triệu gọi bình thường và không cần giẫm chết hay hiến tế bất kỳ đơn vị nào. Nếu cả hai vị trí liền kề đều không hợp lệ, lần triệu gọi thất bại và Brute Call vẫn bị hủy vào `the_void`.

## Điều Kiện Sử Dụng

- Có ít nhất một Goblin Shaman trên `own_frontline`.
- Có Goblin Brute trong `the_void`.
- Goblin 1 hoặc 2 sao đứng liền kề Goblin Shaman **không phải** là điều kiện bắt buộc để kích hoạt kỹ năng.
- Vị trí hợp lệ bên cạnh Goblin Shaman **không phải** là điều kiện bắt buộc để dùng thẻ; nếu không có vị trí hợp lệ, lần triệu gọi thất bại nhưng Brute Call vẫn bị tiêu thụ.

## Cơ Chế & Luồng Thực Thi

1. Chọn Goblin Shaman dùng Brute Call trên `own_frontline`.
2. Kiểm tra hai vị trí liền kề bên trái và bên phải Goblin Shaman.
3. Chọn vị trí Goblin Brute sẽ xuất hiện theo thứ tự ưu tiên:
   - Nếu một hoặc cả hai vị trí đang có thẻ Character thuộc tộc `goblin` từ 1 đến 2 sao, ưu tiên chọn một trong các vị trí đó. Chọn Goblin có số sao thấp nhất; nếu bằng sao, ưu tiên vị trí bên trái.
   - Nếu không có Goblin 1 hoặc 2 sao hợp lệ, chọn một vị trí liền kề đang trống; nếu cả hai đều trống, ưu tiên vị trí bên trái.
4. Nếu không tìm được vị trí hợp lệ, đánh dấu lần triệu gọi thất bại, giữ nguyên Goblin Brute trong `the_void`, bỏ qua bước 5–7 và tiếp tục xử lý bước 8.
5. Nếu vị trí được chọn đang có Goblin 1 hoặc 2 sao, đưa thẻ đó vào `the_void` vì bị Goblin Brute giẫm chết.
6. Triệu gọi Goblin Brute từ `the_void` vào đúng vị trí liền kề vừa được chọn.
7. Goblin Brute vào sân ở trạng thái đã kích hoạt (`trigger = true`).
8. Trong cả trường hợp triệu gọi thành công hoặc thất bại, đưa Brute Call vào `the_void` sau khi xử lý hiệu ứng.

## Ngoại Lệ Cân Bằng

Brute Call là Ability 3 sao nhưng triệu gọi một Character 4 sao và không bắt buộc phải đáp ứng định mức hiến tế 3 sao. Việc Goblin Brute giẫm chết một Goblin 1 hoặc 2 sao chỉ xảy ra khi có mục tiêu hợp lệ đứng cạnh Goblin Shaman; nếu không có mục tiêu nhưng có vị trí liền kề trống, hiệu ứng triệu hồi vẫn được thực hiện. Nếu không có vị trí hợp lệ, lần gọi thất bại và Ability vẫn bị đưa vào `the_void`.
