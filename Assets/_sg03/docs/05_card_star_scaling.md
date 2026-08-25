# 05. Phân Cấp Sức Mạnh Thẻ Bài Theo Cấp Độ Sao

# 🛡️ PHẦN I: PHÂN CẤP SỨC MẠNH THẺ NHÂN VẬT (CHARACTER CARDS)

## 1. Quy Tắc Cân Bằng Cốt Lõi Cho Thẻ Nhân Vật

### Quy Tắc Không 1-Hit Cùng Cấp (Áp Dụng Cho Thẻ 1 Đến 6 Sao)
- Các thẻ Character từ **1 đến 6 sao cùng cấp sao KHÔNG THỂ tiêu diệt lẫn nhau** chỉ bằng 1 đòn tấn công của lá bài đơn lẻ.
- Để tiêu diệt 1 thẻ Character có cấp độ **$N$ sao** ($N \in [1, 6]$), tổng sức mạnh tấn công (tổng số sao phối hợp) phải **ĐẠT TỐI THIỂU $N + 1$ SAO**.
- Luôn đảm bảo: $\max(\text{ATK}_N) < \min(\text{DEF}_N)$ đối với mọi lá bài từ 1 đến 6 sao.

### Quy Tắc Khởi Tạo Thẻ Cao Cấp Đưa Vào Mộ Bài (`the_void`)
- Tất cả các thẻ **Character từ 4 sao trở lên (từ 4 đến 9 sao)** mặc định **SẼ BỊ ĐƯA TRỰC TIẾP VÀO MỘ BÀI (`the_void`) khi khởi tạo trận đấu (`init_cards`)**.
- Các thẻ Character $\ge 4$ sao **KHÔNG THỂ được chọn hoặc rút trực tiếp lên tay (`hand`)** ở đầu trận hay trong các phase rút bài thông thường.
- **Cơ Chế Xuất Hiện**: Người chơi bắt buộc phải sử dụng các thẻ Kỹ năng Chiêu Hồi (`summon_count`) hoặc hiệu ứng đặc biệt từ sới đấu để triệu hồi / hồi sinh các lá Character từ 4 sao trở lên từ `the_void` ra mặt trận.

---

## 2. Nhóm Early Game (1 đến 3 Sao) — Thẻ Nhân Vật (Rút Lên Hand Bình Thường)

| Cấp Độ Sao ($N$) | Khung Sát Thương ATK (Độ Nhảy 10) | Khung Giáp/Máu DEF (Độ Nhảy 10) | Số Sao Công Cần Để Kết Liễu | Khởi Tạo Đầu Game & Ví Dụ Mẫu |
|---|---|---|---|---|
| **1 Sao** | **50 – 150** <br> *(50, 60... 150)* | **160 – 260** <br> *(160, 170... 260)* | **2 Sao** | Rút lên `hand` bình thường.<br>• `Tân Binh A`: ATK 140, DEF 170<br>• `Vệ Sĩ Mẫu B`: ATK 60, DEF 250 |
| **2 Sao** | **150 – 250** <br> *(150, 160... 250)* | **260 – 360** <br> *(260, 270... 360)* | **3 Sao** | Rút lên `hand` bình thường.<br>• `Học Việc Mẫu A`: ATK 230, DEF 280<br>• `Bộ Binh Mẫu B`: ATK 170, DEF 340 |
| **3 Sao** | **250 – 350** <br> *(250, 260... 350)* | **360 – 460** <br> *(360, 370... 460)* | **4 Sao** | Rút lên `hand` bình thường.<br>• `Hiệp Sĩ Mẫu A`: ATK 340, DEF 380<br>• `Đao Phủ Mẫu B`: ATK 270, DEF 440 |

---

## 3. Nhóm Mid Game (4 đến 6 Sao) — Thẻ Nhân Vật (Mặc Định Đưa Vào `the_void`)

| Cấp Độ Sao ($N$) | Khung Sát Thương ATK (Độ Nhảy 10) | Khung Giáp/Máu DEF (Độ Nhảy 10) | Số Sao Công Cần Để Kết Liễu | Khởi Tạo Đầu Game & Ví Dụ Mẫu |
|---|---|---|---|---|
| **4 Sao** | **350 – 450** <br> *(350, 360... 450)* | **460 – 560** <br> *(460, 470... 560)* | **5 Sao** | **Đưa trực tiếp vào `the_void`**.<br>• `Chủ Lực Mẫu A`: ATK 430, DEF 480<br>• `Hỗ Trợ Mẫu B`: ATK 360, DEF 550 |
| **5 Sao** | **450 – 550** <br> *(450, 460... 550)* | **560 – 660** <br> *(560, 570... 660)* | **6 Sao** | **Đưa trực tiếp vào `the_void`**.<br>• `Cuồng Chiến Mẫu A`: ATK 540, DEF 580<br>• `Hoàng Gia Mẫu B`: ATK 460, DEF 650 |
| **6 Sao** | **550 – 650** <br> *(550, 560... 650)* | **660 – 760** <br> *(660, 670... 760)* | **7 Sao** | **Đưa trực tiếp vào `the_void`**.<br>• `Pháp Sư Mẫu A`: ATK 640, DEF 680<br>• `Đại Pháp Sư B`: ATK 560, DEF 750 |

---

## 4. Nhóm Late Game (7 đến 9 Sao) — Thẻ Nhân Vật (Mặc Định Đưa Vào `the_void`)

| Cấp Độ Sao ($N$) | Khung Sát Thương ATK (Độ Nhảy 10) | Khung Giáp/Máu DEF (Độ Nhảy 10) | Số Sao Công Cần Để Kết Liễu | Khởi Tạo Đầu Game & Ví Dụ Mẫu |
|---|---|---|---|---|
| **7 Sao** | **750 – 850** <br> *(750, 760... 850)* | **960 – 1060** <br> *(960, 970... 1060)* | **10 Sao** | **Đưa trực tiếp vào `the_void`**.<br>• `Chỉ Huy Mẫu A`: ATK 840, DEF 970 |
| **8 Sao** | **850 – 950** <br> *(850, 860... 950)* | **1060 – 1160** <br> *(1060, 1070... 1160)* | **11 Sao** | **Đưa trực tiếp vào `the_void`**.<br>• `Tướng Huyền Thoại A`: ATK 940, DEF 1070 |
| **9 Sao** | **950 – 1050** <br> *(950, 960... 1050)* | **1160 – 1260** <br> *(1160, 1170... 1260)* | **12 Sao** | **Đưa trực tiếp vào `the_void`**.<br>• `Đại Tướng Mẫu A`: ATK 1040, DEF 1170 |

---

## 5. Ví Dụ Minh Họa Combat Thẻ Nhân Vật

### ⚔️ Ví Dụ 1 (Early Game): 1-Sao vs 1-Sao
- Thẻ công: `Tân Binh A` (1 Sao: ATK = 140 - Rút từ `hand`).
- Thẻ thủ: `Vệ Sĩ Mẫu B` (1 Sao: DEF = 250 - Rút từ `hand`).
- **Diễn biến**: `Tân Binh A` gây 140 sát thương -> `Vệ Sĩ Mẫu B` còn $250 - 140 = 110$ DEF -> **KHÔNG CHẾT (1-hit)**.
- **Kết quả**: Cần thêm 1 đòn tấn công từ lá 1-sao khác (ví dụ `Trinh Sát C` ATK 100 -> tổng $140 + 100 = 240 \ge 250$) mới kết liễu được.

### ⚔️ Ví Dụ 2 (Mid Game): Triệu Hồi Thẻ 4-Sao Từ `the_void`
- **Khởi tạo**: Lá 4-Sao `Chủ Lực Mẫu A` (ATK 430, DEF 480) nằm sẵn trong mộ bài `the_void` lúc bắt đầu game, không có trên `hand`.
- **Hành động**: Người chơi dùng thẻ Kỹ năng Chiêu Hồi (ví dụ `Chiêu Hồi Mẫu 4` - 4 Sao Ability) để hồi sinh `Chủ Lực Mẫu A` từ `the_void` ra `front_line`.
- **Giao chiến**: `Chủ Lực Mẫu A` tấn công `Hỗ Trợ Mẫu B` (4 Sao đối thủ: DEF 550) -> gây 430 sát thương -> `Hỗ Trợ Mẫu B` còn 120 DEF -> **KHÔNG CHẾT (1-hit)**.

---

# ✨ PHẦN II: PHÂN CẤP SỨC MẠNH THẺ KỸ NĂNG (ABILITY CARDS)

## 1. Quy Tắc Cân Bằng Cốt Lõi Cho Thẻ Kỹ Năng

### Quy Tắc Đơn Chức Năng & Phối Hợp (Single-Function & Synergy Rule)
- **Đơn Chức Năng**: Mỗi thẻ Kỹ năng (`ability`) chỉ sở hữu duy nhất 1 chức năng chiến thuật (tăng ATK, tăng DEF, hoặc Chiêu Hồi đơn vị), KHÔNG kết hợp nhiều tính năng.
- **Cộng Gộp Sức Mạnh Sao**: Khi 1 thẻ Nhân vật **$N$ sao** kết hợp với 1 thẻ Kỹ năng **$S$ sao** (loại tăng ATK/DEF), tổng sức mạnh công hoặc thủ tạo ra tương đương với cấp **$(N + S)$ sao**.

---

## 2. Nhóm Thẻ Kỹ Năng Tăng ATK & DEF (`atk_add` & `def_add`)

| Nhóm Game | Cấp Độ Sao ($S$) | Khung Tăng Sát Thương `atk_add` (Độ Nhảy 10) | Khung Tăng Giáp `def_add` (Độ Nhảy 10) | Ví Dụ Kỹ Năng Mẫu (Giả Định) |
|---|---|---|---|---|
| **Early Game** | **1 Sao** | **50 – 150** *(50, 60... 150)* | **50 – 150** *(50, 60... 150)* | • `Kỹ Năng Trảm Đòn 1`: `atk_add` 100<br>• `Kỹ Năng Khiên 1`: `def_add` 100 |
| | **2 Sao** | **150 – 250** *(150, 160... 250)* | **150 – 250** *(150, 160... 250)* | • `Kỹ Năng Trảm Đòn 2`: `atk_add` 150<br>• `Kỹ Năng Khiên 2`: `def_add` 200 |
| | **3 Sao** | **250 – 350** *(250, 260... 350)* | **250 – 350** *(250, 260... 350)* | • `Kỹ Năng Trảm Đòn 3`: `atk_add` 270<br>• `Kỹ Năng Khiên 3`: `def_add` 300 |
| **Mid Game** | **4 Sao** | **350 – 450** *(350, 360... 450)* | **350 – 450** *(350, 360... 450)* | • `Kỹ Năng Trảm Đòn 4`: `atk_add` 400 |
| | **5 Sao** | **450 – 550** *(450, 460... 550)* | **450 – 550** *(450, 460... 550)* | • `Kỹ Năng Khiên 5`: `def_add` 500 |
| | **6 Sao** | **550 – 650** *(550, 560... 650)* | **550 – 650** *(550, 560... 650)* | • `Kỹ Năng Trảm Đòn 6`: `atk_add` 600 |
| **Late Game** | **7 Sao** | **750 – 850** *(750, 760... 850)* | **750 – 850** *(750, 760... 850)* | • `Kỹ Năng Trảm Đòn 7`: `atk_add` 800 |
| | **8 Sao** | **850 – 950** *(850, 860... 950)* | **850 – 950** *(850, 860... 950)* | • `Kỹ Năng Khiên 8`: `def_add` 900 |
| | **9 Sao** | **950 – 1050** *(950, 960... 1050)* | **950 – 1050** *(950, 960... 1050)* | • `Kỹ Năng Khiên 9`: `def_add` 1000 |

---

## 3. Nhóm Thẻ Kỹ Năng Chiêu Hồi / Triệu Hồi (`summon_count`)

### Quy Tắc Tính Sao Cho Thẻ Chiêu Hồi (Summon Value Rule)
- Cấp độ sao của thẻ Kỹ năng Chiêu Hồi ($S$) bằng **tổng số sao của các đơn vị Nhân vật được tạo ra hoặc trỗi dậy trên sới đấu**:
  $$S = \sum_{i=1}^{\text{summon\_count}} \text{Số Sao của Thẻ được Triệu Hồi}_i$$

### Quy Tắc Miễn Hiến Tế Cho Thẻ Chiêu Hồi Early Game (1 Đến 3 Sao)
- Các thẻ Kỹ năng Chiêu Hồi dùng để triệu hồi / hồi sinh các lá bài Character từ **1 đến 3 Sao** từ **Mộ Bài (`the_void`)** **HOÀN TOÀN KHÔNG CẦN HIẾN TẾ** bất kỳ lá bài hay đơn vị nào trên sới đấu.

### Quy Tắc Định Mức Hiến Tế Tối Thiểu ($K - 1$ Sao Hiến Tế)
- Công thức tổng số sao hiến tế đạt tối thiểu **$K - 1$ Sao** là khung định mức tổng giá trị sao tối thiểu để triệu hồi 1 lá Character **$K$ Sao** ($K \ge 4$) từ **Mộ Bài (`the_void`)**:
  $$\text{Tổng Số Sao Các Đơn Vị Hiến Tế} \ge K - 1$$

#### Ngoại Lệ Được Chỉ Định
- [Brute Call](cards/natureborn/goblin/abilities/brute_call.md) là Ability 3 sao nhưng triệu gọi [Goblin Brute](cards/natureborn/goblin/goblin_brute.md) 4 sao và không bắt buộc phải hiến tế đủ 3 sao. Nếu có Goblin 1 hoặc 2 sao đứng liền kề Goblin Shaman, một đơn vị hợp lệ sẽ bị Goblin Brute giẫm chết; nếu không có, Goblin Brute vẫn được triệu gọi vào một vị trí liền kề đang trống mà không cần hiến tế. Nếu không có vị trí hợp lệ, lần gọi thất bại và Brute Call vẫn bị đưa vào `the_void`. Đây là ngoại lệ đối với cả quy tắc tính sao theo tổng số sao được triệu hồi và định mức hiến tế tối thiểu.

### Quy Tắc Điều Kiện Hiến Tế Riêng Theo Từng Lá Bài (Specific Sacrifice Requirement)
- **Không Hiến Tế Tùy Ý**: Công thức $K - 1$ chỉ là định mức tổng giá trị sao. **Mỗi lá bài Chiêu Hồi đều có quy định điều kiện hiến tế riêng biệt** được mô tả trên từng lá (ví dụ: Yêu cầu vật tế đúng Chủng tộc chỉ định, đúng Tên bài chỉ định, hoặc đúng Loại thẻ nòng cốt). **Không phải cứ gom ngẫu nhiên đủ sao là có thể kích hoạt được**.

### Điều Kiện Hiến Tế Bài Cao Cấp Cho Thẻ 7 Đến 9 Sao (Vật Tế Cốt Lõi từ 4 Đến 6 Sao)
- Để triệu hồi các thẻ Character Huyền Thoại từ **7 đến 9 Sao**, bên cạnh việc đáp ứng định mức $K - 1$ và điều kiện riêng của lá bài, **BẮT BUỘC TRONG ĐỘI HÌNH HIẾN TẾ PHẢI CÓ ÍT NHẤT 1 LÁ BÀI CHARACTER TỪ 4 ĐẾN 6 SAO**:
  $$\text{Chi Trả 7–9 Sao} \implies \text{Tổng Sao} \ge K - 1 \quad \text{AND} \quad \text{Vật tế có ít nhất 1 lá từ } 4\text{–}6 \text{ Sao}$$

### Bảng Phân Cấp Thẻ Chiêu Hồi & Quy Định Hiến Tế:

| Nhóm Game | Cấp Độ Sao Thẻ Chiêu Hồi ($S$) | Đơn Vị Character Được Triệu Hồi Từ `the_void` ($K$) | Tổng Số Sao Tạo Ra | Yêu Cầu Định Mức Sao Hiến Tế | Điều Kiện Thẻ Vật Tế Cốt Lõi & Ví Dụ Mẫu |
|---|---|---|---|---|---|
| **Early Game** | **1 Sao** | 1 đơn vị 1-Sao | 1 Sao | **Không cần hiến tế** | • `Chiêu Hồi Mẫu 1`: Gọi 1 lính 1-sao trực tiếp từ `the_void` |
| | **2 Sao** | 2 đơn vị 1-Sao (hoặc 1 đơn vị 2-Sao) | 2 Sao | **Không cần hiến tế** | • `Chiêu Hồi Mẫu 2`: Gọi 2 lính 1-sao trực tiếp từ `the_void` |
| | **3 Sao** | 3 đơn vị 1-Sao (hoặc 1 đơn vị 3-Sao) | 3 Sao | **Không cần hiến tế** | • `Chiêu Hồi Mẫu 3`: Gọi 3 lính 1-sao từ `the_void` *(Có thể yêu cầu 1 Tướng Chiêu Hồn đứng sân)* |
| **Mid Game** | **4 Sao** | 1 đơn vị Character **4-Sao** | 4 Sao | **3 Sao Hiến Tế** | Hiến tế theo tiêu chuẩn riêng của lá bài *(đạt tổng 3 sao)*. |
| | **5 Sao** | 1 đơn vị Character **5-Sao** | 5 Sao | **4 Sao Hiến Tế** | Hiến tế theo tiêu chuẩn riêng của lá bài *(đạt tổng 4 sao)*. |
| | **6 Sao** | 1 đơn vị Character **6-Sao** | 6 Sao | **5 Sao Hiến Tế** | Hiến tế theo tiêu chuẩn riêng của lá bài *(đạt tổng 5 sao)*. |
| **Late Game** | **7 Sao** | 1 đơn vị Character **7-Sao** | 7 Sao | **6 Sao Hiến Tế** | **Theo tiêu chuẩn riêng + Cần ít nhất 1 lá từ $4$–$6$ sao làm nòng cốt**. |
| | **8 Sao** | 1 đơn vị Character **8-Sao** | 8 Sao | **7 Sao Hiến Tế** | **Theo tiêu chuẩn riêng + Cần ít nhất 1 lá từ $4$–$6$ sao làm nòng cốt**. |
| | **9 Sao** | 1 đơn vị Character **9-Sao** | 9 Sao | **8 Sao Hiến Tế** | **Theo tiêu chuẩn riêng + Cần ít nhất 1 lá từ $4$–$6$ sao làm nòng cốt**. |

---

## 4. Nhóm Thẻ Kỹ Năng Hồi Máu / Phục Hồi (`hp_restore`)

### Quy Tắc Cân Bằng Hồi Máu (Quy Tắc Đối Ứng 1 : 1 Với ATK Character)
- Lượng HP phục hồi của Thẻ Kỹ Năng Hồi Máu cấp **$S$ Sao** tương đương trực tiếp với **Khung Sát Thương Tấn Công ATK của lá bài Character $S$ Sao**:
  $$\text{Khung Hồi Máu } \text{hp\_restore}(S) = \text{Khung Tấn Công } \text{ATK}(S)$$
- **Ý Nghĩa Chiến Thuật**: Thi triển 1 thẻ Hồi Máu cấp $S$ Sao có thể khắc chế / bù đắp chính xác lượng tổn hại sát thương gây ra bởi 1 đòn tấn công từ thẻ Character cùng cấp $S$ Sao.

### Bảng Khung Chỉ Số Thẻ Hồi Máu `hp_restore` (Từ 1 Đến 9 Sao):

| Nhóm Game | Cấp Độ Sao ($S$) | Khung Phục Hồi HP `hp_restore` (Độ Nhảy 10) | Tương Đương Giá Trị ATK Character | Ví Dụ Kỹ Năng Mẫu (Giả Định) |
|---|---|---|---|---|
| **Early Game** | **1 Sao** | **50 – 150 HP** *(50, 60... 150)* | 1-Sao ATK *(50–150)* | • `Kỹ Năng Hồi HP 1`: Phục hồi 100 HP |
| | **2 Sao** | **150 – 250 HP** *(150, 160... 250)* | 2-Sao ATK *(150–250)* | • `Kỹ Năng Hồi HP 2`: Phục hồi 200 HP |
| | **3 Sao** | **250 – 350 HP** *(250, 260... 350)* | 3-Sao ATK *(250–350)* | • `Kỹ Năng Hồi HP 3`: Phục hồi 300 HP |
| **Mid Game** | **4 Sao** | **350 – 450 HP** *(350, 360... 450)* | 4-Sao ATK *(350–450)* | • `Kỹ Năng Hồi HP 4`: Phục hồi 400 HP |
| | **5 Sao** | **450 – 550 HP** *(450, 460... 550)* | 5-Sao ATK *(450–550)* | • `Kỹ Năng Hồi HP 5`: Phục hồi 500 HP |
| | **6 Sao** | **550 – 650 HP** *(550, 650... 650)* | 6-Sao ATK *(550–650)* | • `Kỹ Năng Hồi HP 6`: Phục hồi 600 HP |
| **Late Game** | **7 Sao** | **750 – 850 HP** *(750, 760... 850)* | 7-Sao ATK *(750–850)* | • `Kỹ Năng Hồi HP 7`: Phục hồi 800 HP |
| | **8 Sao** | **850 – 950 HP** *(850, 860... 950)* | 8-Sao ATK *(850–950)* | • `Kỹ Năng Hồi HP 8`: Phục hồi 900 HP |
| | **9 Sao** | **950 – 1050 HP** *(950, 960... 1050)* | 9-Sao ATK *(950–1050)* | • `Kỹ Năng Hồi HP 9`: Phục hồi 1000 HP |

---

## 5. Ví Dụ Minh Họa Phối Hợp Thẻ Kỹ Năng

### ⚔️ Ví Dụ 1: Bài Nhân Vật 1-Sao + Bài Ability 1-Sao Tấn Công (`atk_add`)
- Thẻ công: `Tân Binh A` (1 Sao: ATK = 140).
- Thẻ Kỹ năng bổ trợ: `Kỹ Năng Trảm Đòn 1` (1 Sao Ability: `atk_add` = 150).
- **Tổng Sát Thương Tấn Công**: $\text{ATK} + \text{atk\_add} = 140 + 150 = \mathbf{290}$ (Tương đương đòn đánh 2-sao công).
- Thẻ thủ đối thủ: `Học Việc Mẫu A` (2 Sao: DEF = 280).
- **Diễn biến & Kết quả**: $290 \ge 280 \implies$ **TIÊU DIỆT THÀNH CÔNG THẺ 2-SAO!**

### 💀 Ví Dụ 2: Bài Ability Chiêu Hồi Hồi Sinh Thẻ 7-Sao Từ `the_void` (Yêu Cầu Hiến Tế 6 Sao & Lá Vật Tế Từ $4$–$6$ Sao)
- **Mục tiêu**: Người chơi muốn hồi sinh 1 lá Character 7-Sao `Chỉ Huy Mẫu A` từ **Mộ Bài (`the_void`)**.
- **Tính toán Hiến tế**: Target 7-Sao $\implies$ Cần $7 - 1 = \mathbf{6}$ **Sao hiến tế**, trong đó **bắt buộc phải chứa ít nhất 1 lá từ $4$–$6$ Sao**.
- **Đội hình lựa chọn**: 1 lá 4-Sao + 1 lá 2-Sao (tổng $4 + 2 = 6$ sao) $\implies$ Thỏa mãn cả 2 điều kiện!
- **Diễn biến & Kết quả**: Hiến tế lá 4-sao và lá 2-sao vào `the_void` $\implies$ Triệu hồi thành công lá 7-Sao `Chỉ Huy Mẫu A` ra sân.
