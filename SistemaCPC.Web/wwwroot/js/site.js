// Selección con un clic en las tablas con la clase "seleccionable".
// Al hacer clic en una fila, queda resaltada y sus atributos data-* llenan los campos del formulario
// cuyo id es "f-" + nombre del dato (por ejemplo, data-nombre llena el campo id="f-nombre").
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
    });
});

// Botones con data-limpiar: vacían el formulario y quitan la selección (por ejemplo, "Nuevo afiliado").
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
    });
});
