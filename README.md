# P-EyeRest

P-EyeRest là công cụ nhỏ cho Windows, nhắc bạn nghỉ mắt khi làm việc với máy tính.
Lời nhắc hiện thành một bảng mờ ở giữa màn hình. Chuột bấm xuyên qua bảng này, và bảng không lấy focus bàn phím. Vì vậy công việc đang làm không bị gián đoạn.

## Tính năng

- **Nghỉ ngắn:** mỗi 20 phút làm việc, bảng nhắc bạn nhìn xa ít nhất 6 m trong 20 giây.
- **Nghỉ dài:** mỗi 2 giờ làm việc, bảng nhắc bạn nghỉ 15 phút. Phần đầu có bài đưa mắt theo các hướng (lên, xuống, trái, phải, chéo, đảo tròn, chớp mắt, nhắm mắt). Sau đó bảng mờ hơn và nhắc bạn rời màn hình.
- **Hiện trên mọi màn hình:** mỗi màn hình có một bảng nhắc, nét ở mọi mức scale DPI.
- **Tự dừng đếm khi bạn rời máy:** xem mục [Cách tính thời gian làm việc](#cách-tính-thời-gian-làm-việc).
- **Hoãn nhắc khi bạn đang dùng ứng dụng toàn màn hình:** khi bạn chơi game, trình chiếu hoặc xem video toàn màn hình, P-EyeRest chờ bạn thoát toàn màn hình rồi mới hiện bảng nhắc.
- **Menu ở khay hệ thống:** nghỉ ngay, bỏ qua, xem thử bảng nhắc, chọn kích thước và độ đậm, chỉnh thời gian nhắc, tạm dừng 1 giờ, khởi động cùng Windows.

## Cơ sở của các mốc thời gian

- Nghỉ ngắn theo quy tắc 20-20-20 của Hiệp hội Nhãn khoa Hoa Kỳ (AOA). Một thử nghiệm nhỏ năm 2022 (29 người, 2 tuần) cho thấy nhắc theo quy tắc này làm giảm triệu chứng mỏi mắt và khô mắt. Lợi ích mất đi sau khi ngừng nhắc ([Talens-Estarelles và cộng sự, 2022](https://research.aston.ac.uk/en/publications/the-effects-of-breaks-on-digital-eye-strain-dry-eye-and-binocular/)).
- Nghỉ dài theo khuyến nghị nghỉ màn hình 15 phút sau mỗi 2 giờ sử dụng liên tục ([UMass Memorial Health](https://www.ummhealth.org/health-library/treatment-for-computer-vision-syndrome)).
- Bài đưa mắt theo hướng chỉ để thư giãn. Tác giả chưa tìm thấy nghiên cứu chứng minh bài này có hiệu quả. Nhìn xa và chớp mắt mới là phần chính.

P-EyeRest không phải thiết bị y tế. Nếu mắt bạn đau hoặc mờ kéo dài, bạn nên đi khám bác sĩ mắt.

## Yêu cầu

- Windows 10 (bản 1703 trở lên) hoặc Windows 11.
- .NET Framework 4.x. Windows 10 và 11 đã có sẵn, bạn không phải cài thêm.

## Cài đặt

### Dùng bản build sẵn

1. Tải file `P-EyeRest_v<phiên bản>.zip` ở trang Releases của repo.
2. Chuột phải file zip, chọn **Properties**.
3. Tick **Unblock**, rồi bấm **OK**.
4. Giải nén file zip.
5. Chạy `P-EyeRest.exe`. Icon con mắt hiện ở khay hệ thống.

> **LƯU Ý:** File exe chưa có chữ ký số. Nếu bạn bỏ qua bước 3, Windows SmartScreen có thể hiện cảnh báo "Windows protected your PC". Khi cảnh báo hiện, bấm **More info**, rồi bấm **Run anyway**. Nếu máy bật Smart App Control, Windows có thể chặn file exe chưa ký mà không cho chạy tiếp. Trường hợp này, bạn có thể thử tự build từ mã nguồn. File tự build không mang nhãn "tải từ Internet", nhưng tác giả chưa kiểm cách này trên máy bật Smart App Control.

### Tự build từ mã nguồn

Công cụ dùng trình biên dịch C# có sẵn trong Windows (`csc.exe` của .NET Framework 4.x). Bạn không phải cài Visual Studio hay .NET SDK.

1. Tải mã nguồn của repo.
2. Chạy `build.bat`.
3. Chạy file `P-EyeRest.exe` vừa tạo ra trong cùng thư mục.

## Sử dụng

Chuột phải icon con mắt ở khay hệ thống để mở menu:

| Mục menu | Tác dụng |
|---|---|
| Nghỉ ngắn ngay (20-20-20) | Bắt đầu một lần nghỉ ngắn |
| Nghỉ dài ngay | Bắt đầu một lần nghỉ dài |
| Bỏ qua lần nghỉ đang hiện | Ẩn bảng nhắc và tính lại từ đầu |
| Xem thử overlay | Hiện bảng nhắc 6 giây, không ảnh hưởng bộ đếm |
| Kích thước overlay | 50%, 70% (mặc định), 85% hoặc 92% vùng làm việc của màn hình |
| Độ đậm overlay | 35%, 55%, 75% (mặc định) hoặc 90% |
| Thời gian nhắc | Chọn các mốc thời gian và cách tính rời máy, xem bảng bên dưới |
| Tạm dừng 1 giờ | Dừng nhắc trong 1 giờ. Bấm lại để tiếp tục |
| Khởi động cùng Windows | Bật hoặc tắt shortcut trong thư mục Startup của bạn |
| Giới thiệu P-EyeRest | Xem phiên bản, tác giả và giấy phép |
| Thoát | Tắt P-EyeRest |

Menu **Thời gian nhắc** có các mục sau. Lựa chọn mới có hiệu lực ngay, bạn không phải khởi động lại P-EyeRest.

| Mục menu | Lựa chọn |
|---|---|
| Nghỉ ngắn mỗi | 15, 20 (mặc định), 30 hoặc 45 phút |
| Nghỉ ngắn trong | 20 (mặc định), 30 hoặc 60 giây |
| Nghỉ dài mỗi | 1 giờ, 1 giờ 30 phút, 2 giờ (mặc định), 3 giờ, hoặc tắt nghỉ dài |
| Nghỉ dài trong | 5, 10 hoặc 15 phút (mặc định) |
| Coi là rời máy khi không chạm máy | 10, 15 (mặc định) hoặc 30 phút, hoặc "Không bao giờ" (chỉ tính rời máy khi khóa máy hoặc tắt màn hình) |
| Hoãn nhắc khi đang toàn màn hình | Bật (mặc định) hoặc tắt |

Rê chuột lên icon để xem còn bao lâu tới lần nghỉ kế tiếp.

## Cấu hình

P-EyeRest lưu các lựa chọn của bạn trong file `%APPDATA%\P-EyeRest\settings.ini`. Mỗi dòng có dạng `khóa=số`, ví dụ:

```ini
fill=0.7
micro_every_min=20
opacity=0.75
```

| Khóa | Ý nghĩa | Khoảng cho phép | Mặc định |
|---|---|---|---|
| `fill` | Tỉ lệ vùng làm việc mà bảng nhắc chiếm | 0.30 tới 0.95 | 0.7 |
| `opacity` | Độ đậm của bảng nhắc | 0.20 tới 0.90 | 0.75 |
| `micro_every_min` | Số phút làm việc giữa hai lần nghỉ ngắn | 5 tới 120 | 20 |
| `micro_sec` | Số giây của một lần nghỉ ngắn | 10 tới 120 | 20 |
| `long_every_min` | Số phút làm việc giữa hai lần nghỉ dài. `0` là tắt nghỉ dài | 0 tới 480 | 120 |
| `long_min` | Số phút của một lần nghỉ dài | 2 tới 60 | 15 |
| `away_after_min` | Số phút không chạm máy thì coi là rời máy. `0` là chỉ tính khi khóa máy hoặc tắt màn hình | 0 tới 120 | 15 |
| `defer_fullscreen` | `1` là hoãn nhắc khi đang toàn màn hình, `0` là không hoãn | 0 tới 1 | 1 |

Menu khay chỉ đưa ra vài lựa chọn. Để dùng giá trị khác trong khoảng cho phép, sửa file này khi P-EyeRest đã tắt, rồi mở lại P-EyeRest. Nếu một giá trị nằm ngoài khoảng cho phép, P-EyeRest dùng giá trị mặc định.

## Cách tính thời gian làm việc

Không chạm phím hoặc chuột chưa chắc là bạn đang nghỉ, vì bạn có thể đang xem video hoặc đang đọc. P-EyeRest chỉ dừng đếm khi một trong các điều kiện sau đúng:

- Máy khóa, screensaver đang chạy, hoặc màn hình đã tắt.
- Bạn không chạm phím và chuột ít nhất 15 phút (chỉnh được trong menu **Thời gian nhắc**), không có video đang phát, và không có ứng dụng toàn màn hình hoặc chế độ trình chiếu.

P-EyeRest biết có video đang phát nhờ Windows: khi phát video, trình duyệt và trình phát video yêu cầu Windows giữ màn hình sáng. Video tắt tiếng cũng có yêu cầu này. Nhạc phát không có hình thì không có yêu cầu này, nên nhạc nền không làm P-EyeRest coi là bạn đang xem.

Nếu bạn rời máy ít nhất 5 phút, bộ đếm nghỉ ngắn quay về 0. Nếu bạn rời máy ít nhất bằng thời lượng nghỉ dài (mặc định 15 phút), bộ đếm nghỉ dài cũng quay về 0. Khi máy sleep hoặc hibernate, P-EyeRest tính thời gian ngủ như thời gian rời máy.

## Giới hạn hiện tại

- P-EyeRest không có cảm biến để biết bạn có đang nhìn màn hình hay không. Nếu bạn thường đọc lâu mà không chạm máy, chọn "Không bao giờ" ở mục **Coi là rời máy khi không chạm máy**. Khi đó P-EyeRest chỉ tính rời máy khi máy khóa hoặc màn hình tắt.
- Một số ứng dụng giữ màn hình sáng cả khi không phát video, ví dụ công cụ chống sleep hoặc phần mềm điều khiển từ xa. Khi ứng dụng này chạy, P-EyeRest coi là bạn đang xem, nên không dừng đếm khi bạn không chạm máy.
- Trình phát video dùng cách giữ màn hình sáng kiểu cũ (`SetThreadExecutionState`) thì P-EyeRest không thấy được. Tác giả đã kiểm với Edge, chưa kiểm với trình phát video khác.
- Tác giả đã kiểm chế độ hoãn nhắc với cửa sổ không viền phủ toàn màn hình, chưa kiểm với game DirectX độc quyền hoặc PowerPoint trình chiếu.

## Giấy phép

Copyright (C) 2026 Phúc

P-EyeRest là phần mềm tự do, phát hành theo [GNU General Public License v3.0 hoặc mới hơn](LICENSE) (GPL-3.0-or-later). Bạn được dùng, sửa và phát hành lại. Khi phát hành bản đã sửa, bạn phải công bố mã nguồn theo cùng giấy phép. Phần mềm được cung cấp nguyên trạng, không có bảo hành.
