document.addEventListener("DOMContentLoaded", function () {

    // İçeriği fade-in ile göster
    const content = document.querySelector(".content-card");
    if (content) {
        content.style.opacity = 0;
        content.style.transform = "translateY(20px)";

        setTimeout(() => {
            content.style.transition = "all 0.6s ease";
            content.style.opacity = 1;
            content.style.transform = "translateY(0)";
        }, 150);
    }

});

CKEDITOR.replace('editor', {
    height: 400
});
<script src="https://cdn.ckeditor.com/4.16.2/standard/ckeditor.js"></script>
