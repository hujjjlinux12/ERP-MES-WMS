const $ = selector => document.querySelector(selector);

async function api(url, method = "GET", body) {
    const options = { method };

    if (body !== undefined) {
        options.headers = { "Content-Type": "application/json" };
        options.body = JSON.stringify(body);
    }

    const response = await fetch(url, options);
    const text = await response.text();
    const isJson = response.headers.get("Content-Type")?.includes("json");
    const data = text && isJson ? JSON.parse(text) : text;

    if (!response.ok) {
        const detail = typeof data === "string"
            ? data
            : data?.detail || data?.title;
        throw new Error(`HTTP ${response.status}${detail ? ` · ${detail}` : ""}`);
    }

    return data;
}

function report(selector, text, isError = false) {
    const message = $(selector);
    message.textContent = text;
    message.dataset.error = String(isError);
}

function appendRow(table, values) {
    const row = table.insertRow();

    for (const value of values) {
        row.insertCell().textContent = value;
    }

    return row;
}

function addButton(cell, label, action) {
    const button = document.createElement("button");
    button.type = "button";
    button.textContent = label;
    button.addEventListener("click", action);
    cell.append(button);
}

const materialForm = $("#material-form");
const materialFields = materialForm.elements;
let editingCode = null;

function resetMaterialForm() {
    editingCode = null;
    materialForm.reset();
    materialFields.code.readOnly = false;
    $("#material-save").textContent = "新增物料";
    $("#material-cancel").hidden = true;
}

function materialUrl(code) {
    return `/api/materials/${encodeURIComponent(code)}`;
}

async function materialAction(action) {
    try {
        await action();
    } catch (error) {
        report("#material-message", error.message, true);
    }
}

async function loadMaterials() {
    const state = $("#material-filter").value;
    const suffix = state === "" ? "" : `?isActive=${state}`;
    const materials = await api(`/api/materials${suffix}`);
    const table = $("#material-rows");
    table.replaceChildren();

    for (const material of materials) {
        const { code, name, unit, isActive } = material;
        const row = appendRow(table, [code, name, unit, isActive ? "已启用" : "已停用"]);
        const actions = row.insertCell();
        const url = materialUrl(code);

        addButton(actions, "查看 / 编辑", () => materialAction(async () => {
            const detail = await api(url);
            editingCode = detail.code;
            materialFields.code.value = detail.code;
            materialFields.name.value = detail.name;
            materialFields.unit.value = detail.unit;
            materialFields.code.readOnly = true;
            $("#material-save").textContent = "保存修改";
            $("#material-cancel").hidden = false;
            report("#material-message", `正在编辑 ${code}`);
            materialFields.name.focus();
        }));

        addButton(actions, isActive ? "停用" : "启用", () => materialAction(async () => {
            await api(`${url}/status`, "PUT", { isActive: !isActive });
            await loadMaterials();
            report("#material-message", `${code} 的状态已更新。`);
        }));

        addButton(actions, "删除", () => materialAction(async () => {
            if (!confirm(`删除物料 ${code}？`)) return;

            await api(url, "DELETE");
            if (editingCode === code) resetMaterialForm();
            await loadMaterials();
            report("#material-message", `${code} 已删除。`);
        }));
    }

    if (materials.length === 0) {
        appendRow(table, ["没有符合条件的物料"]).cells[0].colSpan = 5;
    }
}

materialForm.addEventListener("submit", event => {
    event.preventDefault();

    materialAction(async () => {
        const save = $("#material-save");
        save.disabled = true;

        try {
            const name = materialFields.name.value.trim();
            const unit = materialFields.unit.value.trim();

            if (editingCode === null) {
                const code = materialFields.code.value.trim();
                await api("/api/materials", "POST", { code, name, unit });
            } else {
                await api(materialUrl(editingCode), "PUT", { name, unit });
            }

            resetMaterialForm();
            await loadMaterials();
            report("#material-message", "物料已保存。");
        } finally {
            save.disabled = false;
        }
    });
});

$("#material-cancel").addEventListener("click", resetMaterialForm);
$("#material-filter").addEventListener("change", () => materialAction(loadMaterials));

async function loadSuppliers() {
    const suppliers = await api("/api/suppliers");
    const table = $("#supplier-rows");
    table.replaceChildren();

    for (const supplier of suppliers) {
        appendRow(table, [supplier.code, supplier.name]);
    }

    if (suppliers.length === 0) {
        appendRow(table, ["还没有供应商"]).cells[0].colSpan = 2;
    }
}

$("#supplier-form").addEventListener("submit", async event => {
    event.preventDefault();

    const form = event.currentTarget;
    const save = form.querySelector("button");
    save.disabled = true;

    try {
        await api("/api/suppliers", "POST", {
            code: form.elements.code.value.trim(),
            name: form.elements.name.value.trim()
        });

        form.reset();
        await loadSuppliers();
        report("#supplier-message", "供应商已保存。");
    } catch (error) {
        report("#supplier-message", error.message, true);
    } finally {
        save.disabled = false;
    }
});

materialAction(loadMaterials);
loadSuppliers().catch(error => report("#supplier-message", error.message, true));