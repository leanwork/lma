/* Lean Modular Architecture — Leanwork Group */
(function () {
  "use strict";

  var $ = function (s, c) { return (c || document).querySelector(s); };
  var $$ = function (s, c) { return Array.prototype.slice.call((c || document).querySelectorAll(s)); };

  /* ---------------------------------------------------------- Ano */

  var ano = $("#ano");
  if (ano) ano.textContent = String(new Date().getFullYear());

  /* ---------------------------------------------------------- Navegação */

  var nav = $("#nav");
  var toggle = $("#navToggle");
  var drawer = $("#navDrawer");

  function onScroll() {
    nav.classList.toggle("is-stuck", window.scrollY > 8);
    spy();
  }

  function closeDrawer() {
    drawer.classList.remove("is-open");
    toggle.setAttribute("aria-expanded", "false");
    toggle.setAttribute("aria-label", "Abrir menu");
    document.body.classList.remove("is-locked");
  }

  if (toggle && drawer) {
    toggle.addEventListener("click", function () {
      var open = drawer.classList.toggle("is-open");
      toggle.setAttribute("aria-expanded", String(open));
      toggle.setAttribute("aria-label", open ? "Fechar menu" : "Abrir menu");
      document.body.classList.toggle("is-locked", open);
    });
    $$("a", drawer).forEach(function (a) { a.addEventListener("click", closeDrawer); });
    document.addEventListener("keydown", function (e) {
      if (e.key === "Escape" && drawer.classList.contains("is-open")) {
        closeDrawer();
        toggle.focus();
      }
    });
  }

  /* Destaque da seção corrente */

  var links = $$(".nav-links a");
  var targets = links
    .map(function (a) { return { link: a, el: $(a.getAttribute("href")) }; })
    .filter(function (t) { return t.el; });

  function spy() {
    var line = window.scrollY + (nav ? nav.offsetHeight : 0) + 120;
    var current = null;
    targets.forEach(function (t) {
      if (t.el.offsetTop <= line) current = t;
    });
    // Na última rolagem, marca a última seção visível
    if (window.innerHeight + window.scrollY >= document.body.offsetHeight - 4) {
      current = targets[targets.length - 1] || current;
    }
    targets.forEach(function (t) {
      t.link.classList.toggle("is-active", t === current);
    });
  }

  window.addEventListener("scroll", onScroll, { passive: true });
  window.addEventListener("resize", spy, { passive: true });
  onScroll();

  /* ---------------------------------------------------------- Abas */

  $$('[role="tablist"]').forEach(function (list) {
    var tabs = $$('[role="tab"]', list);

    function select(tab) {
      tabs.forEach(function (t) {
        var on = t === tab;
        t.setAttribute("aria-selected", String(on));
        t.tabIndex = on ? 0 : -1;
        var panel = document.getElementById(t.getAttribute("aria-controls"));
        if (panel) panel.hidden = !on;
      });
    }

    tabs.forEach(function (tab, i) {
      tab.addEventListener("click", function () { select(tab); });
      tab.addEventListener("keydown", function (e) {
        var next = e.key === "ArrowRight" ? i + 1 : e.key === "ArrowLeft" ? i - 1 : null;
        if (next === null) return;
        e.preventDefault();
        var target = tabs[(next + tabs.length) % tabs.length];
        select(target);
        target.focus();
      });
    });
  });

  /* ---------------------------------------------------------- Copiar */

  $$(".copy").forEach(function (btn) {
    var original = btn.lastChild;
    btn.addEventListener("click", function () {
      var block = btn.closest(".code");
      var code = block && $("pre", block);
      if (!code) return;
      navigator.clipboard.writeText(code.textContent.trim()).then(function () {
        original.textContent = " Copiado";
        setTimeout(function () { original.textContent = " Copiar"; }, 1800);
      }).catch(function () {
        original.textContent = " Erro";
        setTimeout(function () { original.textContent = " Copiar"; }, 1800);
      });
    });
  });

  /* ---------------------------------------------------------- Revelação */

  var revealables = $$(".reveal");
  if ("IntersectionObserver" in window && !matchMedia("(prefers-reduced-motion: reduce)").matches) {
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (!entry.isIntersecting) return;
        entry.target.classList.add("is-in");
        io.unobserve(entry.target);
      });
    }, { rootMargin: "0px 0px -10% 0px", threshold: 0.05 });
    revealables.forEach(function (el) { io.observe(el); });
  } else {
    revealables.forEach(function (el) { el.classList.add("is-in"); });
  }

  /* ---------------------------------------------------------- Realce sintático */

  var KEYWORDS = ("abstract as async await base bool break byte case catch char class const continue decimal " +
    "default delegate do double else enum event explicit extern false finally fixed float for foreach get " +
    "goto if implicit in int interface internal is lock long namespace new null object operator out override " +
    "params private protected public readonly record ref return sbyte sealed set short sizeof stackalloc " +
    "static string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using var " +
    "virtual void volatile where while yield").split(" ");

  var BASH_KEYWORDS = ("cd git dotnet docker compose npm cp mv mkdir export echo").split(" ");

  var PATTERNS = {
    csharp: /(\/\/[^\n]*|\/\*[\s\S]*?\*\/)|([$@]?"(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*')|(\b\d[\w.]*\b)|([A-Za-z_]\w*)|([{}()[\];])/g,
    bash: /(#[^\n]*)|("(?:\\.|[^"\\])*"|'[^']*')|(\b\d[\w.]*\b)|([A-Za-z_][\w-]*)|([{}()[\];])/g
  };

  function escapeHtml(s) {
    return s.replace(/[&<>]/g, function (c) {
      return c === "&" ? "&amp;" : c === "<" ? "&lt;" : "&gt;";
    });
  }

  function highlight(source, lang) {
    var re = PATTERNS[lang];
    if (!re) return escapeHtml(source);
    re.lastIndex = 0;

    var out = "";
    var last = 0;
    var m;

    while ((m = re.exec(source)) !== null) {
      out += escapeHtml(source.slice(last, m.index));
      last = re.lastIndex;

      var text = escapeHtml(m[0]);
      if (m[1]) out += '<span class="tok-com">' + text + "</span>";
      else if (m[2]) out += '<span class="tok-str">' + text + "</span>";
      else if (m[3]) out += '<span class="tok-num">' + text + "</span>";
      else if (m[5]) out += '<span class="tok-pun">' + text + "</span>";
      else if (m[4]) {
        var word = m[4];
        var isKeyword = lang === "bash"
          ? BASH_KEYWORDS.indexOf(word) !== -1
          : KEYWORDS.indexOf(word) !== -1;
        if (isKeyword) out += '<span class="tok-key">' + text + "</span>";
        else if (lang === "csharp" && /^[A-Z]/.test(word)) out += '<span class="tok-typ">' + text + "</span>";
        else out += text;
      }
    }

    return out + escapeHtml(source.slice(last));
  }

  $$("code[data-lang]").forEach(function (el) {
    el.innerHTML = highlight(el.textContent, el.getAttribute("data-lang"));
  });
})();
