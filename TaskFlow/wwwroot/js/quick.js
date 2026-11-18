// The Quick list (Chapter 26): the browser calls TaskFlow's JSON API with fetch().
const list = document.getElementById("quick-list");
const message = document.getElementById("quick-message");
const token = document.querySelector("input[name='__RequestVerificationToken']").value;

// GET /api/tasks, then draw one <li> per task.
async function loadTasks() {
    const response = await fetch("/api/tasks");
    const tasks = await response.json();
    list.replaceChildren();
    for (const task of tasks) {
        const item = document.createElement("li");
        item.className = "list-group-item d-flex justify-content-between align-items-center";
        const title = document.createElement("span");
        title.textContent = task.title;           // textContent, never innerHTML: titles are text
        if (task.done) title.className = "text-decoration-line-through text-muted";
        item.append(title);
        if (!task.done) {
            const button = document.createElement("button");
            button.className = "btn btn-sm btn-outline-success";
            button.textContent = "Done";
            button.addEventListener("click", () => markDone(task.id));
            item.append(button);
        }
        list.append(item);
    }
}

// POST /api/tasks with a JSON body. 201 means created; 400 lists what was wrong.
async function addTask(title) {
    const response = await fetch("/api/tasks", {
        method: "POST",
        headers: { "Content-Type": "application/json", "X-CSRF-TOKEN": token },
        body: JSON.stringify({ title: title, priority: "Medium" })
    });
    if (response.status === 201) {
        message.textContent = "";
        await loadTasks();
        return true;
    }
    const problem = await response.json();     // ProblemDetails: title, status, errors
    message.textContent = Object.values(problem.errors ?? {}).flat().join(" ") || problem.title;
    return false;
}

// POST /api/tasks/{id}/done: 204, nothing to read.
async function markDone(id) {
    await fetch(`/api/tasks/${id}/done`, { method: "POST", headers: { "X-CSRF-TOKEN": token } });
    await loadTasks();
}

document.getElementById("quick-add").addEventListener("submit", async event => {
    event.preventDefault();                    // stay on this page: no form post
    const box = document.getElementById("quick-title");
    if (await addTask(box.value)) box.value = "";
});

loadTasks();
