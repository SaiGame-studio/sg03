# Lightning Strike

- **Mã Thẻ Bài**: `lightning_strike`
- **Loại Thẻ**: [`ability`](../../../../../04_abilities.md)
- **Số sao**: 3
- **Chủng Tộc Chính**: **Lightborn**
- **Tộc Nhánh**: Light Elf (định danh kỹ thuật: `light_elf`)
- **Thẻ Nhân Vật Yêu Cầu**: [Azura](../azura.md)

## Mô Tả Kỹ Năng
Lightning Strike chỉ có thể được triển khai bởi Azura. Cô dồn linh lực sấm sét vào quyền thuật rồi đánh lan từ Character đối thủ được chọn sang một Character liền kề trên cùng battle line, gây cho mỗi mục tiêu lượng sát thương bằng `base_stats.atk` của Lightning Strike.

Với Character 1 hoặc 2 sao, dòng điện khiến mục tiêu mất đúng lượt kế tiếp của phe mình: mọi planning attack đang chờ của mục tiêu bị hủy và, nếu đang trong một đòn tấn công chờ xử lý, mục tiêu trở về holder của nó. Character từ 3 sao trở lên chỉ nhận sát thương `base_stats.atk`; chúng không bị bỏ lượt, không bị hủy planning attack và không bị đưa về holder bởi Lightning Strike.

## Điều Kiện Sử Dụng
- Azura phải là Character triển khai Lightning Strike.
- Chọn một Character đối thủ có một Character khác đứng liền kề trên cùng battle line; kỹ năng tấn công cả hai.

## Hiệu Quả
- Mỗi mục tiêu nhận sát thương bằng `base_stats.atk` của Lightning Strike.
- Mục tiêu 1 hoặc 2 sao hủy planning attack, bỏ đúng lượt kế tiếp của phe mình và trở về holder nếu đang trong đòn tấn công chờ xử lý.
- Mục tiêu từ 3 sao trở lên chỉ nhận sát thương.
