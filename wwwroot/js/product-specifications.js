(() => {
    const toggle = document.getElementById("HasPhoneSpecification");
    const fields = document.getElementById("phone-spec-fields");
    if (!toggle || !fields) return;
    const requiredIds = ["PhoneChipset", "PhoneRamGb", "PhoneStorageGb", "PhoneScreenSizeInches"];
    function update() {
        fields.hidden = !toggle.checked;
        fields.disabled = !toggle.checked;
        toggle.setAttribute("aria-expanded", String(toggle.checked));
        requiredIds.forEach(id => {
            const input = document.getElementById(id);
            if (input) input.required = toggle.checked;
        });
    }
    toggle.addEventListener("change", update);
    update();
})();
