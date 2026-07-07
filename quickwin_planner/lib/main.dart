import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  runApp(const QuickWinApp());
}

class QuickWinApp extends StatelessWidget {
  const QuickWinApp({super.key});

  @override
  Widget build(BuildContext context) {
    const seed = Color(0xFF0E7C66);

    return MaterialApp(
      title: 'QuickWin Planner',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        useMaterial3: true,
        colorScheme: ColorScheme.fromSeed(
          seedColor: seed,
          brightness: Brightness.light,
        ),
        scaffoldBackgroundColor: const Color(0xFFF5F7F4),
        cardTheme: CardThemeData(
          color: Colors.white,
          elevation: 0,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(18),
            side: const BorderSide(color: Color(0xFFE2E8DF)),
          ),
        ),
      ),
      home: const PlannerHomePage(),
    );
  }
}

class PlannerTask {
  PlannerTask({
    required this.id,
    required this.title,
    required this.createdAt,
    this.done = false,
  });

  final String id;
  String title;
  bool done;
  final DateTime createdAt;

  Map<String, dynamic> toJson() => {
        'id': id,
        'title': title,
        'done': done,
        'createdAt': createdAt.toIso8601String(),
      };

  static PlannerTask fromJson(Map<String, dynamic> json) => PlannerTask(
        id: json['id'] as String,
        title: json['title'] as String,
        done: json['done'] as bool? ?? false,
        createdAt: DateTime.tryParse(json['createdAt'] as String? ?? '') ??
            DateTime.now(),
      );
}

class PlannerHomePage extends StatefulWidget {
  const PlannerHomePage({super.key});

  @override
  State<PlannerHomePage> createState() => _PlannerHomePageState();
}

class _PlannerHomePageState extends State<PlannerHomePage> {
  static const _storageKey = 'quickwin_tasks_v1';
  static const _lastOpenedKey = 'quickwin_last_opened';
  static const _streakKey = 'quickwin_streak';

  final List<PlannerTask> _tasks = [];
  int _streak = 1;
  bool _loaded = false;

  int get _completed => _tasks.where((task) => task.done).length;
  int get _open => _tasks.length - _completed;
  double get _progress => _tasks.isEmpty ? 0 : _completed / _tasks.length;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    final prefs = await SharedPreferences.getInstance();
    final rawTasks = prefs.getString(_storageKey);
    final savedStreak = prefs.getInt(_streakKey) ?? 1;
    final lastOpened = prefs.getString(_lastOpenedKey);

    final today = DateUtils.dateOnly(DateTime.now());
    final previous = DateTime.tryParse(lastOpened ?? '');
    var nextStreak = savedStreak;

    if (previous != null) {
      final previousDay = DateUtils.dateOnly(previous);
      final difference = today.difference(previousDay).inDays;
      if (difference == 1) {
        nextStreak++;
      } else if (difference > 1) {
        nextStreak = 1;
      }
    }

    final loadedTasks = <PlannerTask>[];
    if (rawTasks != null) {
      final decoded = jsonDecode(rawTasks) as List<dynamic>;
      loadedTasks.addAll(
        decoded
            .cast<Map<String, dynamic>>()
            .map(PlannerTask.fromJson)
            .where((task) => DateUtils.isSameDay(task.createdAt, today)),
      );
    }

    if (loadedTasks.isEmpty) {
      loadedTasks.addAll(_starterTasks());
    }

    setState(() {
      _tasks
        ..clear()
        ..addAll(loadedTasks);
      _streak = nextStreak;
      _loaded = true;
    });

    await prefs.setInt(_streakKey, nextStreak);
    await prefs.setString(_lastOpenedKey, DateTime.now().toIso8601String());
    await _save();
  }

  List<PlannerTask> _starterTasks() {
    return [
      _newTask('Pick one priority for today'),
      _newTask('Do a 10-minute focused work sprint'),
      _newTask('Review what went well'),
    ];
  }

  PlannerTask _newTask(String title) {
    return PlannerTask(
      id: DateTime.now().microsecondsSinceEpoch.toString(),
      title: title.trim(),
      createdAt: DateTime.now(),
    );
  }

  Future<void> _save() async {
    final prefs = await SharedPreferences.getInstance();
    final raw = jsonEncode(_tasks.map((task) => task.toJson()).toList());
    await prefs.setString(_storageKey, raw);
  }

  Future<void> _showTaskSheet({PlannerTask? existing}) async {
    final controller = TextEditingController(text: existing?.title ?? '');
    final isEditing = existing != null;

    final result = await showModalBottomSheet<String>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (context) {
        return Padding(
          padding: EdgeInsets.only(
            left: 20,
            right: 20,
            bottom: MediaQuery.of(context).viewInsets.bottom + 20,
            top: 8,
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                isEditing ? 'Edit quick win' : 'Add quick win',
                style: Theme.of(context).textTheme.titleLarge?.copyWith(
                      fontWeight: FontWeight.w800,
                    ),
              ),
              const SizedBox(height: 14),
              TextField(
                controller: controller,
                autofocus: true,
                textInputAction: TextInputAction.done,
                decoration: const InputDecoration(
                  labelText: 'Task',
                  border: OutlineInputBorder(),
                ),
                onSubmitted: (value) => Navigator.pop(context, value),
              ),
              const SizedBox(height: 14),
              FilledButton.icon(
                onPressed: () => Navigator.pop(context, controller.text),
                icon: Icon(isEditing ? Icons.save : Icons.add_task),
                label: Text(isEditing ? 'Save' : 'Add task'),
              ),
            ],
          ),
        );
      },
    );

    final text = result?.trim();
    if (text == null || text.isEmpty) return;

    setState(() {
      if (existing == null) {
        _tasks.insert(0, _newTask(text));
      } else {
        existing.title = text;
      }
    });
    await _save();
  }

  Future<void> _toggleTask(PlannerTask task) async {
    setState(() => task.done = !task.done);
    await _save();
  }

  Future<void> _deleteTask(PlannerTask task) async {
    setState(() => _tasks.remove(task));
    await _save();
  }

  Future<void> _resetToday() async {
    setState(() {
      _tasks
        ..clear()
        ..addAll(_starterTasks());
    });
    await _save();
  }

  @override
  Widget build(BuildContext context) {
    if (!_loaded) {
      return const Scaffold(
        body: Center(child: CircularProgressIndicator()),
      );
    }

    return Scaffold(
      appBar: AppBar(
        title: const Text('QuickWin Planner'),
        actions: [
          IconButton(
            tooltip: 'Reset today',
            onPressed: _resetToday,
            icon: const Icon(Icons.refresh),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => _showTaskSheet(),
        icon: const Icon(Icons.add),
        label: const Text('Add'),
      ),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 100),
          children: [
            _HeaderCard(
              progress: _progress,
              completed: _completed,
              total: _tasks.length,
              open: _open,
              streak: _streak,
            ),
            const SizedBox(height: 16),
            Text(
              'Today',
              style: Theme.of(context).textTheme.titleLarge?.copyWith(
                    fontWeight: FontWeight.w800,
                  ),
            ),
            const SizedBox(height: 10),
            if (_tasks.isEmpty)
              const _EmptyState()
            else
              ..._tasks.map(
                (task) => _TaskTile(
                  task: task,
                  onToggle: () => _toggleTask(task),
                  onEdit: () => _showTaskSheet(existing: task),
                  onDelete: () => _deleteTask(task),
                ),
              ),
            const SizedBox(height: 12),
            _SuggestionPanel(
              onPick: (title) async {
                setState(() => _tasks.insert(0, _newTask(title)));
                await _save();
              },
            ),
          ],
        ),
      ),
    );
  }
}

class _HeaderCard extends StatelessWidget {
  const _HeaderCard({
    required this.progress,
    required this.completed,
    required this.total,
    required this.open,
    required this.streak,
  });

  final double progress;
  final int completed;
  final int total;
  final int open;
  final int streak;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(18),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    'Win the day in small steps',
                    style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                          fontWeight: FontWeight.w900,
                        ),
                  ),
                ),
                CircleAvatar(
                  radius: 28,
                  backgroundColor:
                      Theme.of(context).colorScheme.primaryContainer,
                  child: Text(
                    '${(progress * 100).round()}%',
                    style: const TextStyle(fontWeight: FontWeight.w900),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            LinearProgressIndicator(
              value: progress,
              minHeight: 10,
              borderRadius: BorderRadius.circular(99),
            ),
            const SizedBox(height: 16),
            Row(
              children: [
                _Metric(label: 'Done', value: '$completed/$total'),
                _Metric(label: 'Open', value: '$open'),
                _Metric(label: 'Streak', value: '$streak d'),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _Metric extends StatelessWidget {
  const _Metric({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            value,
            style: Theme.of(context).textTheme.titleLarge?.copyWith(
                  fontWeight: FontWeight.w900,
                ),
          ),
          Text(label),
        ],
      ),
    );
  }
}

class _TaskTile extends StatelessWidget {
  const _TaskTile({
    required this.task,
    required this.onToggle,
    required this.onEdit,
    required this.onDelete,
  });

  final PlannerTask task;
  final VoidCallback onToggle;
  final VoidCallback onEdit;
  final VoidCallback onDelete;

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      child: ListTile(
        leading: Checkbox(
          value: task.done,
          onChanged: (_) => onToggle(),
        ),
        title: Text(
          task.title,
          style: TextStyle(
            fontWeight: FontWeight.w700,
            decoration: task.done ? TextDecoration.lineThrough : null,
            color: task.done ? Colors.black54 : null,
          ),
        ),
        trailing: PopupMenuButton<String>(
          onSelected: (value) {
            if (value == 'edit') onEdit();
            if (value == 'delete') onDelete();
          },
          itemBuilder: (context) => const [
            PopupMenuItem(value: 'edit', child: Text('Edit')),
            PopupMenuItem(value: 'delete', child: Text('Delete')),
          ],
        ),
        onTap: onToggle,
      ),
    );
  }
}

class _SuggestionPanel extends StatelessWidget {
  const _SuggestionPanel({required this.onPick});

  final ValueChanged<String> onPick;

  static const suggestions = [
    'Drink water',
    'Walk for 10 minutes',
    'Clear one small admin task',
    'Send one follow-up message',
    'Plan tomorrow before bed',
  ];

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Quick add',
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.w800,
                  ),
            ),
            const SizedBox(height: 10),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: suggestions
                  .map(
                    (item) => ActionChip(
                      avatar: const Icon(Icons.bolt, size: 18),
                      label: Text(item),
                      onPressed: () => onPick(item),
                    ),
                  )
                  .toList(),
            ),
          ],
        ),
      ),
    );
  }
}

class _EmptyState extends StatelessWidget {
  const _EmptyState();

  @override
  Widget build(BuildContext context) {
    return const Card(
      child: Padding(
        padding: EdgeInsets.all(24),
        child: Center(
          child: Text('No tasks yet. Add your first quick win.'),
        ),
      ),
    );
  }
}
