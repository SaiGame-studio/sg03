# Titan Fall (Titan Giáng Lâm)

- **Loại Thẻ**: [`ability`](../../../04_abilities.md)
- **Số sao**: 5
- **Chủng Tộc**: `human` (Nhân Loại)
- **Thẻ Nhân Vật Được Triệu Hồi**: [Titan](../titan.md)

## Mô Tả Kỹ Năng

Khi chiến tuyến Human đứng trước hiểm nguy, Ren gọi Titan xuất hiện để đáp lại lời hứa bảo vệ cô của người cha đã tạo ra nó. Bước chân của người máy khổng lồ làm rung chuyển chiến trường, biến thời khắc tưởng như tuyệt vọng thành cơ hội để phe bạn tái lập thế trận.

## Điều Kiện Sử Dụng

- Trên sân phe bạn phải có ít nhất một [Ren](../azure_blade.md), bất kể trạng thái `trigger`.
- Mục tiêu của Titan Fall phải là một thẻ Character thuộc chủng tộc `human` đang bị tấn công.
- Thẻ Human mục tiêu phải đã nhận tổng cộng DEF buff không thấp hơn `base_stats.def_buff_required` của Titan Fall (hiện tại là **+100**).
- Sát thương gây ra bởi đòn tấn công phải vượt qua toàn bộ phòng thủ của mục tiêu, theo công thức: **`atk + accumulated_damage > def + def_add`**.

Titan Fall chỉ được kích hoạt khi tất cả các điều kiện trên đều được thỏa mãn.

## Hiệu Quả

Khi kích hoạt, Titan Fall đưa thẻ Human mục tiêu vào `the_void`, sau đó triệu gọi [Titan](../titan.md) từ `the_void` vào đúng vị trí mà thẻ Human vừa rời khỏi trên sân phe bạn. Titan được triệu hồi ở trạng thái `trigger`.
