Don't bother touching line endings and BOMs.
Let tidy_code handle that as needed.

When I say "build" or "test" run `./build-and-test.cmd`.
Also, automatically do this before each commit.

## Secrets

Always obscure API keys and other secrets, especially in saved screenshots.

## C#

A `switch` should only ever have a single statement in a case.
If it's going to be more than that, extract.

Avoid ternary (`?:`) except for very simple cases.

Don't use `sealed` on classes or records.

The most important thing about a name is that it not be misleading.
A reader should never need to look at how a variable or method is used/implemented to understand what it represents.
It is OK to make the name long to achieve this.
If you see a comment explaining what a function or variable does/is, rename it to make it more obvious.

## Committing

When I say "commit", first run `./build-and-test.cmd`.
If the change has more than one thing going on, split it into multiple commits, each with a clear purpose.
Especially separate refactoring from behavior changes.

## After every task

At the end of every task report on all of these, briefly:
- Skills used: name them.
- Skill gaps: if a skill you wished existed would have helped, propose one, even briefly.
- Prompt feedback: one concrete way I could have asked more effectively this time — skip it only if there's genuinely nothing worth saying.
- Questions, ideas, concerns: surface them as they come up, not just at the end.
- Changes to your skills annd isntructions that would reduce token usage or improve efficiency.

Treat this end-of-task report as mandatory, like a checklist item — don't let it get crowded out by concise-output-style guidance; it's a small fixed addition, not a violation of terseness.

Information about this project found in @InternalDocuments/project.md

Never use Claude Code's persistent memory system for this project. Durable guidance (conventions, process corrections, preferences) belongs in-repo instead — this file, `InternalDocuments/`, or a skill — so it's visible in version control and to every contributor, not just stored privately for one user.

## Decisions
When you need input on a decision:
- First, pick the option you think is best and propose it. Start that message with `⭐ 1: `
- Also list the alternatives you rejected, each with a brief reason why. Start each with `❌ n:`
Then have a discussion.
This formatting applies every time you present a decision, even a quick one — don't let a short or informal-feeling exchange skip it.
