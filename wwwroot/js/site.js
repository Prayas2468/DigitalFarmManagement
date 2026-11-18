(() => {
  const sidebar = document.querySelector(".sidebar");
  const scrim = document.querySelector(".scrim");
  const openMenu = () => {
    sidebar?.classList.add("open");
    scrim?.classList.add("show");
  };
  const closeMenu = () => {
    sidebar?.classList.remove("open");
    scrim?.classList.remove("show");
  };
  document.getElementById("menuBtn")?.addEventListener("click", openMenu);
  scrim?.addEventListener("click", closeMenu);

  const modalEl = document.getElementById("confirmModal");
  const confirmText = document.getElementById("confirmText");
  const confirmYes = document.getElementById("confirmYes");
  let pending = null;
  if (modalEl && window.bootstrap) {
    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    document.addEventListener("submit", (event) => {
      const form = event.target;
      if (!(form instanceof HTMLFormElement) || !form.dataset.confirm || form.dataset.confirmed === "1")
        return;
      event.preventDefault();
      pending = form;
      if (confirmText) confirmText.textContent = form.dataset.confirm;
      modal.show();
    });
    confirmYes?.addEventListener("click", () => {
      if (!pending) return;
      pending.dataset.confirmed = "1";
      modal.hide();
      pending.submit();
    });
  }

  document.querySelectorAll("canvas[data-chart]").forEach((canvas) => {
    if (!window.Chart) return;
    const kind = canvas.dataset.chart;
    const labels = JSON.parse(canvas.dataset.labels || "[]");
    const values = JSON.parse(canvas.dataset.values || "[]");
    const colors = ["#204632", "#416d4a", "#789568", "#a7bd88", "#c5d3a3", "#d7dfb3", "#9b3933", "#c5a453"];
    new Chart(canvas, {
      type: kind === "pie" ? "doughnut" : "bar",
      data: {
        labels,
        datasets: [{
          data: values,
          backgroundColor: kind === "pie" ? ["#416d4a", "#9b3933", "#c5a453"] : colors,
          borderRadius: kind === "pie" ? 0 : 8,
          maxBarThickness: 42
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: kind === "pie", position: "bottom" } },
        scales: kind === "pie" ? {} : { y: { beginAtZero: true, ticks: { precision: 0 } }, x: { grid: { display: false } } }
      }
    });
  });
})();
