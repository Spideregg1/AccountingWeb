const configuredApiBase = document.body?.dataset?.apiBase?.trim();
const isHttpProtocol = window.location.protocol.startsWith("http");

const resolvedApiBase = configuredApiBase && configuredApiBase.length > 0
  ? configuredApiBase
  : isHttpProtocol
    ? window.location.origin
    : "http://localhost:5000";
const API_BASE = resolvedApiBase.replace(/\/$/, "");

const filterForm = document.getElementById("filter-form");
const resetFiltersBtn = document.getElementById("reset-filters");
const transactionForm = document.getElementById("transaction-form");
const cancelEditBtn = document.getElementById("cancel-edit");
const tableBody = document.getElementById("transaction-body");
const template = document.getElementById("transaction-row");
const resultCount = document.getElementById("result-count");
const calendarContainer = document.getElementById("calendar");
const calendarLabel = document.getElementById("calendar-label");
const prevMonthBtn = document.getElementById("prev-month");
const nextMonthBtn = document.getElementById("next-month");
const formTitle = document.getElementById("form-title");

const state = {
  editingId: null,
  calendarDate: new Date(),
};

const formFields = {
  id: transactionForm.elements.namedItem("id"),
  date: transactionForm.elements.namedItem("date"),
  type: transactionForm.elements.namedItem("type"),
  category: transactionForm.elements.namedItem("category"),
  amount: transactionForm.elements.namedItem("amount"),
  note: transactionForm.elements.namedItem("note"),
};

const typeMap = {
  1: "收入",
  2: "支出",
  3: "轉帳",
};

formFields.date.value = new Date().toISOString().slice(0, 10);

async function fetchJson(url, options = {}) {
  let res;

  try {
    res = await fetch(url, {
      headers: {
        "Content-Type": "application/json",
      },
      ...options,
    });
  } catch (error) {
    throw new Error(`無法連線至 API (${API_BASE}): ${error.message}`);
  }

  if (!res.ok) {
    const message = await res.text();
    throw new Error(message || "系統錯誤");
  }

  if (res.status === 204) {
    return null;
  }
  return res.json();
}

function getFilterParams() {
  const data = new FormData(filterForm);
  const params = new URLSearchParams();
  for (const [key, value] of data.entries()) {
    if (value) {
      params.append(key, value);
    }
  }
  return params;
}

async function loadTransactions() {
  try {
    const params = getFilterParams();
    const data = await fetchJson(`${API_BASE}/api/transactions?${params.toString()}`);
    renderTransactions(data);
  } catch (error) {
    alert(`讀取資料失敗: ${error.message}`);
  }
}

function renderTransactions(items) {
  tableBody.innerHTML = "";
  resultCount.textContent = `${items.length} 筆`;

  items.forEach((item) => {
    const clone = template.content.cloneNode(true);
    clone.querySelector('[data-field="date"]').textContent = item.date;
    const typeCell = clone.querySelector('[data-field="type"]');
    const typeLabel = typeMap[item.type] || item.type;
    typeCell.innerHTML = `<span class="type-pill type-${item.type}">${typeLabel}</span>`;
    clone.querySelector('[data-field="category"]').textContent = item.category;
    const amountCell = clone.querySelector('[data-field="amount"]');
    amountCell.classList.add("amount");
    amountCell.dataset.intent = item.type === 1 ? "income" : item.type === 2 ? "expense" : "transfer";
    amountCell.textContent = Number(item.amount).toLocaleString("zh-TW", {
      style: "currency",
      currency: "TWD",
      minimumFractionDigits: 0,
    });
    clone.querySelector('[data-field="note"]').textContent = item.note || "-";

    const row = clone.querySelector("tr");
    row.dataset.id = item.id;
    tableBody.appendChild(clone);
  });
}

transactionForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  const formData = new FormData(transactionForm);
  const payload = Object.fromEntries(formData.entries());
  payload.amount = Number(payload.amount);
  payload.type = Number(payload.type);

  const isEditing = Boolean(state.editingId);
  const url = isEditing
    ? `${API_BASE}/api/transactions/${state.editingId}`
    : `${API_BASE}/api/transactions`;
  const method = isEditing ? "PUT" : "POST";

  if (isEditing) {
    payload.id = state.editingId;
  }

  try {
    await fetchJson(url, {
      method,
      body: JSON.stringify(payload),
    });
    resetForm();
    await Promise.all([loadTransactions(), loadCalendar()]);
  } catch (error) {
    alert(`儲存失敗: ${error.message}`);
  }
});

function resetForm() {
  transactionForm.reset();
  formFields.date.value = new Date().toISOString().slice(0, 10);
  formFields.id.value = "";
  formTitle.textContent = "新增紀錄";
  state.editingId = null;
}

cancelEditBtn.addEventListener("click", () => {
  resetForm();
});

filterForm.addEventListener("submit", (event) => {
  event.preventDefault();
  loadTransactions();
});

resetFiltersBtn.addEventListener("click", () => {
  filterForm.reset();
  loadTransactions();
});

tableBody.addEventListener("click", async (event) => {
  const button = event.target.closest("button");
  if (!button) return;

  const row = button.closest("tr");
  const id = row?.dataset.id;
  if (!id) return;

  if (button.dataset.action === "edit") {
    startEdit(id);
  } else if (button.dataset.action === "delete") {
    if (confirm("確定刪除這筆紀錄嗎？")) {
      try {
        await fetchJson(`${API_BASE}/api/transactions/${id}`, { method: "DELETE" });
        await Promise.all([loadTransactions(), loadCalendar()]);
      } catch (error) {
        alert(`刪除失敗: ${error.message}`);
      }
    }
  }
});

async function startEdit(id) {
  try {
    const item = await fetchJson(`${API_BASE}/api/transactions/${id}`);
    formFields.id.value = item.id;
    formFields.date.value = item.date;
    formFields.type.value = item.type;
    formFields.category.value = item.category;
    formFields.amount.value = item.amount;
    formFields.note.value = item.note || "";

    state.editingId = item.id;
    formTitle.textContent = "編輯紀錄";
  } catch (error) {
    alert(`讀取紀錄失敗: ${error.message}`);
  }
}

prevMonthBtn.addEventListener("click", () => {
  state.calendarDate.setMonth(state.calendarDate.getMonth() - 1);
  loadCalendar();
});

nextMonthBtn.addEventListener("click", () => {
  state.calendarDate.setMonth(state.calendarDate.getMonth() + 1);
  loadCalendar();
});

async function loadCalendar() {
  const year = state.calendarDate.getFullYear();
  const month = state.calendarDate.getMonth() + 1;
  calendarLabel.textContent = `${year} / ${month.toString().padStart(2, "0")}`;

  try {
    const summaries = await fetchJson(
      `${API_BASE}/api/transactions/calendar?year=${year}&month=${month}`
    );
    renderCalendar(summaries);
  } catch (error) {
    console.error(error);
  }
}

function renderCalendar(summaries) {
  const firstDay = new Date(state.calendarDate.getFullYear(), state.calendarDate.getMonth(), 1);
  const lastDay = new Date(state.calendarDate.getFullYear(), state.calendarDate.getMonth() + 1, 0);
  const startOffset = firstDay.getDay();
  const map = new Map(summaries.map((s) => [s.date, s]));
  const weekdayNames = ["日", "一", "二", "三", "四", "五", "六"];
  const todayStr = new Date().toISOString().slice(0, 10);

  calendarContainer.innerHTML = "";
  weekdayNames.forEach((day) => {
    const header = document.createElement("div");
    header.textContent = day;
    header.className = "day weekday";
    header.style.fontWeight = "600";
    header.style.background = "transparent";
    header.style.border = "none";
    calendarContainer.appendChild(header);
  });

  for (let i = 0; i < startOffset; i += 1) {
    const placeholder = document.createElement("div");
    placeholder.className = "day placeholder";
    calendarContainer.appendChild(placeholder);
  }

  for (let day = 1; day <= lastDay.getDate(); day += 1) {
    const dateStr = `${state.calendarDate.getFullYear()}-${String(
      state.calendarDate.getMonth() + 1
    ).padStart(2, "0")}-${String(day).padStart(2, "0")}`;
    const summary = map.get(dateStr);

    const cell = document.createElement("div");
    cell.className = "day";
    cell.dataset.date = dateStr;
    if (dateStr === todayStr) {
      cell.classList.add("today");
    }

    const dateEl = document.createElement("div");
    dateEl.className = "date";
    dateEl.textContent = day;

    const summaryEl = document.createElement("div");
    summaryEl.className = "summary";

    if (summary) {
      cell.classList.add("has-records");
      summaryEl.innerHTML = `共 ${summary.count} 筆<br />淨額 ${summary.netAmount.toFixed(0)}`;
      summaryEl.classList.add(summary.netAmount >= 0 ? "net-positive" : "net-negative");
    } else {
      summaryEl.textContent = "無紀錄";
    }

    cell.appendChild(dateEl);
    cell.appendChild(summaryEl);

    cell.addEventListener("click", () => {
      filterForm.startDate.value = dateStr;
      filterForm.endDate.value = dateStr;
      loadTransactions();
    });

    calendarContainer.appendChild(cell);
  }
}

async function init() {
  await Promise.all([loadTransactions(), loadCalendar()]);
}

init();
