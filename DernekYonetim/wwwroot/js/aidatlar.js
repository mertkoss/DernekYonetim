document.addEventListener("DOMContentLoaded", function () {
    console.log("Aidat listesi yüklendi");

    // Tablo satırlarına tıklanabilirlik efekti
    const tableRows = document.querySelectorAll('#aidatTable tbody tr');
    tableRows.forEach(row => {
        row.style.cursor = 'pointer';
        row.addEventListener('click', function () {
            const memberId = this.querySelector('td:first-child').textContent;
            console.log(`Üye ${memberId} aidat detayına gidiliyor...`);
            // Burada detay sayfasına yönlendirme yapılabilir
        });
    });

    // Durum filtreleme (örnek)
    const statusFilter = document.createElement('select');
    statusFilter.className = 'filter-select';
    statusFilter.innerHTML = `
                <option value="all">Tüm Durumlar</option>
                <option value="paid">Ödenmiş</option>
                <option value="unpaid">Ödenmemiş</option>
            `;

    // Filtreleme fonksiyonu
    statusFilter.addEventListener('change', function () {
        const status = this.value;
        tableRows.forEach(row => {
            const statusBadge = row.querySelector('.status-badge');
            if (status === 'all') {
                row.style.display = '';
            } else if (status === 'paid' && statusBadge.classList.contains('status-paid')) {
                row.style.display = '';
            } else if (status === 'unpaid' && statusBadge.classList.contains('status-unpaid')) {
                row.style.display = '';
            } else {
                row.style.display = 'none';
            }
        });
    });

    // Filtreyi ekle
    const cardHeader = document.querySelector('.card-header');
    const filtersDiv = document.createElement('div');
    filtersDiv.className = 'filters';
    filtersDiv.appendChild(statusFilter);
    cardHeader.appendChild(filtersDiv);
});