document.querySelectorAll("table.seleccionable tbody tr").forEach(fila => {
    fila.addEventListener("click", () => {
        fila.closest("tbody").querySelectorAll("tr").forEach(f => f.classList.remove("seleccionada"));
        fila.classList.add("seleccionada");

        for (const [campo, valor] of Object.entries(fila.dataset)) {
            const control = document.getElementById("f-" + campo);
            if (!control) continue;
            if (control.type === "checkbox") {
                control.checked = valor === "true";
            } else {
                control.value = valor;
            }
        }

        document.querySelectorAll("[data-solo-al-crear]").forEach(control => control.readOnly = true);
    });
});

document.querySelectorAll("[data-limpiar]").forEach(boton => {
    boton.addEventListener("click", () => {
        document.querySelectorAll("tr.seleccionada").forEach(f => f.classList.remove("seleccionada"));
        document.querySelectorAll("[id^='f-']").forEach(control => {
            if (control.type === "checkbox") {
                control.checked = false;
            } else if (control.tagName === "SELECT") {
                control.selectedIndex = 0;
            } else {
                control.value = "";
            }
        });

        document.querySelectorAll("[data-solo-al-crear]").forEach(control => control.readOnly = false);
    });
});
