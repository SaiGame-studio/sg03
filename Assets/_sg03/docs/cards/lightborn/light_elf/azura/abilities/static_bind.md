# Static Bind

- **Mã Thẻ Bài**: `static_bind`
- **Loại Thẻ**: [`ability`](../../../../../04_abilities.md)
- **Số sao**: 4
- **Chủng Tộc Chính**: **Lightborn**
- **Tộc Nhánh**: Light Elf (định danh kỹ thuật: `light_elf`)
- **Vị Trí Nhắm Mục Tiêu**: Hàng trước hoặc hàng sau đối thủ
- **Thẻ Nhân Vật Yêu Cầu**: [Azura](../azura.md)
- **Mã Nhân Vật Yêu Cầu**: `volt_heart`

## Mô Tả Kỹ Năng
Azura giải phóng điện quang từ quyền thuật để trói một Character đối thủ bằng dòng điện tĩnh. Mục tiêu bị lộ diện, chịu lượng sát thương `stun_damage` được cấu hình trên chính lá Static Bind, và không thể thực hiện đòn tấn công đã được xếp hàng: toàn bộ kế hoạch tấn công của mục tiêu bị hủy. Nếu mục tiêu đang lao tới trong một đòn tấn công chờ xử lý và vẫn còn trên sân sau sát thương, nó trở về vị trí giữ thẻ của mình.

## Điều Kiện Sử Dụng
- Lá Static Bind phải đang ở trên một battle line của phe bạn.
- Azura phải có mặt, chưa hành động, ở hàng trước của phe bạn.
- Chọn một Character đối thủ ở hàng trước hoặc hàng sau.
- `base_stats.stun_damage` của Static Bind phải là số dương.

## Hiệu Quả
- Azura được tính là đã hành động và được lộ diện.
- Character mục tiêu được lộ diện, nhận sát thương `stun_damage`, và mọi kế hoạch tấn công đang chờ của nó bị xóa.
- Sau khi hiệu ứng hoàn tất, Static Bind được đưa vào `the_void` của phe bạn.
